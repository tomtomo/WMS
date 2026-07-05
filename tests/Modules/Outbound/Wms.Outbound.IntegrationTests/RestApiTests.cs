using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// Kontrak REST /v1 Outbound
[Collection(PostgresCollection.Name)]
public sealed class RestApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await OutboundTestHost.MigrateAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Create_wave_via_rest_returns_created_and_exposes_detail_and_queue()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_app.Services, "SKU-MILK", 10m);
        var warehouseId = Guid.NewGuid();

        var create = await _client.PostAsJsonAsync("/v1/waves", new { orderIds = new[] { orderId }, warehouseId });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var waveId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("waveId").GetGuid();

        var detail = await _client.GetFromJsonAsync<JsonElement>($"/v1/waves/{waveId}");
        detail.GetProperty("status").GetString().Should().Be("Active");
        detail.GetProperty("warehouseId").GetGuid().Should().Be(warehouseId);

        var queue = await _client.GetFromJsonAsync<JsonElement>($"/v1/waves?warehouseId={warehouseId}&status=Active");
        queue.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Dispatch_before_ready_is_problem_409_with_error_code()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_app.Services);
        var create = await _client.PostAsJsonAsync("/v1/waves", new { orderIds = new[] { orderId }, warehouseId = Guid.NewGuid() });
        var waveId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("waveId").GetGuid();

        var dispatch = await _client.PostAsync($"/v1/waves/{waveId}/dispatch", content: null);

        dispatch.StatusCode.Should().Be(HttpStatusCode.Conflict);
        dispatch.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await dispatch.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("errorCode").GetString().Should().Be("wave.not_ready");
    }

    [Fact]
    public async Task Complete_picking_then_dispatch_via_rest_closes_the_order()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_app.Services, "SKU-MILK", 10m);
        var create = await _client.PostAsJsonAsync("/v1/waves", new { orderIds = new[] { orderId }, warehouseId = Guid.NewGuid() });
        var waveId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("waveId").GetGuid();

        await PipelineRunner.ConsumeAsync(
            _app.Services,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid())),
            Guid.NewGuid());

        var worklist = await _client.GetFromJsonAsync<JsonElement>(
            $"/v1/picking-tasks?assignedTo={FakePickAssignmentPolicy.Picker}");
        worklist.GetArrayLength().Should().Be(1);
        var taskId = worklist[0].GetProperty("pickingTaskId").GetGuid();
        var qty = worklist[0].GetProperty("qty").GetDecimal();

        var complete = await _client.PostAsJsonAsync(
            $"/v1/picking-tasks/{taskId}/complete",
            new { actualQty = qty, stagingLocationId = Guid.NewGuid(), operatorId = Guid.NewGuid() });
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetFromJsonAsync<JsonElement>($"/v1/waves/{waveId}")).GetProperty("status").GetString()
            .Should().Be("Ready");

        var dispatch = await _client.PostAsync($"/v1/waves/{waveId}/dispatch", content: null);
        dispatch.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var backlog = await _client.GetFromJsonAsync<JsonElement>("/v1/outbound-orders/backlog");
        backlog.GetArrayLength().Should().Be(0, "order terpenuhi semua. Closed, tak balik backlog");
    }
}
