using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Kontrak REST /v1
[Collection(PostgresCollection.Name)]
public sealed class RestApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await InboundTestHost.MigrateAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Post_create_201_dengan_body_camelcase()
    {
        var response = await _client.PostAsJsonAsync("/v1/goods-receipts", CreateBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var raw = await response.Content.ReadAsStringAsync();
        raw.Should().Contain("\"goodsReceiptId\"", "kontrak JSON camelCase (Web defaults)");
    }

    [Fact]
    public async Task Alur_penuh_rest_a1_sampai_confirm()
    {
        var grId = await CreateViaRestAsync();

        (await _client.PostAsJsonAsync(
            $"/v1/goods-receipts/{grId}/scans",
            new { sku = "SKU-A", actualQty = 12, lineStatus = "good" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent, "lineStatus case-insensitive by name");

        (await _client.PostAsync($"/v1/goods-receipts/{grId}/complete-scan", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Review SPV
        var review = await _client.GetFromJsonAsync<JsonElement>($"/v1/goods-receipts/{grId}/review");
        review.GetProperty("hasOverDelivery").GetBoolean().Should().BeTrue();
        review.GetProperty("unresolvedCount").GetInt32().Should().Be(1);
        var group = review.GetProperty("discrepancyGroups")[0];
        group.GetProperty("sku").GetString().Should().Be("SKU-A");
        group.GetProperty("type").GetString().Should().Be("OverDelivery");
        var discrepancyId = group.GetProperty("items")[0].GetProperty("discrepancyId").GetGuid();

        // Antrean pending memuat GR ini.
        var pending = await _client.GetFromJsonAsync<JsonElement>("/v1/goods-receipts/pending");
        pending.GetProperty("totalCount").GetInt32().Should().Be(1);
        pending.GetProperty("items")[0].GetProperty("goodsReceiptId").GetGuid().Should().Be(grId);

        (await _client.PostAsJsonAsync(
            $"/v1/goods-receipts/{grId}/discrepancies/{discrepancyId}/resolution",
            new { action = "RejectExcess", note = "kelebihan ditolak" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.PostAsync($"/v1/goods-receipts/{grId}/confirm", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await _client.GetFromJsonAsync<JsonElement>($"/v1/goods-receipts/{grId}");
        detail.GetProperty("status").GetString().Should().Be("Confirmed");
        detail.GetProperty("receivedLines").GetArrayLength().Should().Be(1);
        detail.GetProperty("rejectedLines").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Conflict_bisnis_menjadi_problem_409_dengan_error_code()
    {
        var grId = await CreateViaRestAsync();

        var response = await _client.PostAsync($"/v1/goods-receipts/{grId}/confirm", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("errorCode").GetString().Should().Be("goods_receipt.not_pending");
        problem.GetProperty("detail").GetString().Should().NotBeNullOrEmpty();
        problem.TryGetProperty("correlationId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Validasi_gagal_menjadi_problem_400()
    {
        var response = await _client.PostAsJsonAsync("/v1/goods-receipts", new
        {
            poRef = string.Empty,
            supplierId = Guid.NewGuid(),
            warehouseId = Guid.NewGuid(),
            dockDoor = "DOCK-1",
            expectedLines = new[] { new { sku = "SKU-A", expectedQty = 1, uom = "EA" } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("validation.failed");
    }

    [Fact]
    public async Task Idempotency_key_replay_tanpa_eksekusi_ganda()
    {
        using var first = BuildCreateRequest("key-123");
        var firstResponse = await _client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstId = firstBody.GetProperty("goodsReceiptId").GetGuid();

        using var retry = BuildCreateRequest("key-123");
        var retryResponse = await _client.SendAsync(retry);
        retryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var retryBody = await retryResponse.Content.ReadFromJsonAsync<JsonElement>();
        retryBody.GetProperty("goodsReceiptId").GetGuid().Should().Be(firstId, "HIT = replay respons tersimpan");

        using var fresh = BuildCreateRequest("key-456");
        var freshBody = await (await _client.SendAsync(fresh)).Content.ReadFromJsonAsync<JsonElement>();
        freshBody.GetProperty("goodsReceiptId").GetGuid().Should().NotBe(firstId);

        // Hanya dua GR yang terbentuk
        var pendingTotal = await PipelineRunner.QueryDbAsync(_app.Services, async context =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
                context.Set<Wms.Inbound.Domain.GoodsReceipt>()));
        pendingTotal.Should().Be(2);
    }

    [Fact]
    public async Task Idempotency_key_duplikat_konkuren_satu_eksekusi()
    {
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(async _ =>
            {
                using var request = BuildCreateRequest("key-race");
                return await _client.SendAsync(request);
            }));

        // Pemenang 201; yang kalah boleh 409 (masih pending) atau 201 replay (sudah completed).
        responses.Should().OnlyContain(response =>
            response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.Conflict);
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.Created);

        var total = await PipelineRunner.QueryDbAsync(_app.Services, async context =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(
                context.Set<Wms.Inbound.Domain.GoodsReceipt>()));
        total.Should().Be(1, "duplikat konkuren tidak boleh menghasilkan side-effect ganda");
    }

    [Fact]
    public async Task Upload_dan_download_url_attachment_via_rest()
    {
        var grId = await CreateViaRestAsync();

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(file, "file", "surat-jalan.pdf");

        var uploadResponse = await _client.PostAsync($"/v1/goods-receipts/{grId}/attachments", form);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var attachmentId = (await uploadResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("attachmentId").GetGuid();

        var listed = await _client.GetFromJsonAsync<JsonElement>($"/v1/goods-receipts/{grId}/attachments");
        listed.GetArrayLength().Should().Be(1);
        listed[0].GetProperty("fileName").GetString().Should().Be("surat-jalan.pdf");

        var downloadUrl = await _client.GetFromJsonAsync<JsonElement>(
            $"/v1/goods-receipts/{grId}/attachments/{attachmentId}/download-url");
        downloadUrl.GetProperty("url").GetString().Should().Contain("sig=");

        (await _client.DeleteAsync($"/v1/goods-receipts/{grId}/attachments/{attachmentId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static object CreateBody() => new
    {
        poRef = "PO-REST-1",
        supplierId = GoodsReceiptScenarios.SupplierId,
        warehouseId = GoodsReceiptScenarios.WarehouseId,
        dockDoor = "DOCK-1",
        expectedLines = new[] { new { sku = "SKU-A", expectedQty = 10, uom = "EA" } },
    };

    private HttpRequestMessage BuildCreateRequest(string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/goods-receipts")
        {
            Content = JsonContent.Create(CreateBody()),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

    private async Task<Guid> CreateViaRestAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/goods-receipts", CreateBody());
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("goodsReceiptId").GetGuid();
    }
}
