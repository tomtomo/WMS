using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.MasterData.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

// REST management /v1/ — camelCase
[Collection(PostgresCollection.Name)]
public sealed class RestApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await MasterDataTestHost.MigrateAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Create_warehouse_returns_201_and_get_returns_camel_case_body()
    {
        var create = await _client.PostAsJsonAsync("/v1/warehouses", new { name = "DC Jakarta Cakung", address = "Jl. Raya Cakung No. 1" });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await create.Content.ReadFromJsonAsync<JsonElement>();
        var warehouseId = createdBody.GetProperty("warehouseId").GetGuid();

        var get = await _client.GetAsync($"/v1/warehouses/{warehouseId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await get.Content.ReadAsStringAsync();
        body.Should().Contain("\"name\"").And.Contain("\"isActive\"", "JSON minimal-API camelCase");
    }

    [Fact]
    public async Task Create_warehouse_with_blank_name_returns_400_problem_details()
    {
        var response = await _client.PostAsJsonAsync("/v1/warehouses", new { name = string.Empty, address = "Jl. Cakung" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Duplicate_sku_returns_409_conflict()
    {
        var product = new { sku = "SKU-DUP", name = "Dup", uom = "piece", batchTrackingRequired = false, expiryTrackingRequired = false, qcRequiredOnReceipt = false, shelfLifeDays = (int?)null };
        (await _client.PostAsJsonAsync("/v1/products", product)).StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await _client.PostAsJsonAsync("/v1/products", product);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_unknown_warehouse_returns_404()
    {
        var response = await _client.GetAsync($"/v1/warehouses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_warehouse_returns_204_then_hidden_from_get()
    {
        var create = await _client.PostAsJsonAsync("/v1/warehouses", new { name = "DC Temp", address = "Jl. Temp" });
        var warehouseId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("warehouseId").GetGuid();

        var delete = await _client.DeleteAsync($"/v1/warehouses/{warehouseId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/v1/warehouses/{warehouseId}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound, "soft-deleted tersembunyi dari lookup default");
    }
}
