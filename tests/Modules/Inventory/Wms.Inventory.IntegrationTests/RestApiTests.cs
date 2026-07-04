using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Kontrak REST /v1 Inventory — read via read port.
[Collection(PostgresCollection.Name)]
public sealed class RestApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await InventoryTestHost.MigrateAsync(_app.Services);
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task Complete_putaway_via_rest_moves_stock_to_available()
    {
        await ReceiveAsync();
        var taskId = await SingleQueuedTaskIdAsync();
        var destination = Guid.NewGuid();

        var complete = await _client.PostAsJsonAsync(
            $"/v1/putaway-tasks/{taskId}/complete",
            new { actualDestinationId = destination, operatorId = Guid.NewGuid() });
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stock = await _client.GetFromJsonAsync<JsonElement>(
            $"/v1/stock?warehouseId={GrConfirmedFactory.WarehouseId}");
        stock.GetArrayLength().Should().Be(1);
        stock[0].GetProperty("status").GetString().Should().Be("Available");
        stock[0].GetProperty("availableQty").GetDecimal().Should().Be(100m);
        stock[0].GetProperty("locationId").GetGuid().Should().Be(destination);
    }

    [Fact]
    public async Task Get_stock_excludes_on_hand_before_putaway()
    {
        await ReceiveAsync();

        var raw = await (await _client.GetAsync($"/v1/stock?warehouseId={GrConfirmedFactory.WarehouseId}"))
            .Content.ReadAsStringAsync();
        raw.Should().Be("[]", "balance OnHand belum Available");
    }

    [Fact]
    public async Task Complete_illegal_transition_is_problem_409_with_error_code()
    {
        await ReceiveAsync();
        var taskId = await SingleQueuedTaskIdAsync();

        (await _client.PostAsJsonAsync(
            $"/v1/putaway-tasks/{taskId}/complete",
            new { actualDestinationId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await _client.PostAsJsonAsync(
            $"/v1/putaway-tasks/{taskId}/complete",
            new { actualDestinationId = Guid.NewGuid() });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        second.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(409);
        problem.GetProperty("errorCode").GetString().Should().Be("putaway_task.not_assigned");
    }

    private async Task ReceiveAsync() =>
        (await PipelineRunner.ConsumeAsync(
            _app.Services,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.Good(qty: 100m)),
            Guid.NewGuid())).IsSuccess.Should().BeTrue();

    private async Task<Guid> SingleQueuedTaskIdAsync()
    {
        var tasks = await _client.GetFromJsonAsync<JsonElement>("/v1/putaway-tasks");
        tasks.GetArrayLength().Should().Be(1);
        return tasks[0].GetProperty("putawayTaskId").GetGuid();
    }
}
