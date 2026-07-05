using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Outbound.Infrastructure;
using Wms.Outbound.Infrastructure.Saga;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class OutboundSagaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = OutboundTestHost.Build(connectionString);
        await OutboundTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Starting_a_saga_persists_state_in_the_outbound_schema_via_an_outbound_owned_orchestrator()
    {
        var sagaId = "wave-cancel-" + Guid.NewGuid().ToString("N");

        using (var scope = _provider.CreateScope())
        {
            var orchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();
            orchestrator.GetType().FullName.Should().StartWith(
                "Wms.Outbound.Infrastructure", "orchestrator Outbound-owned, bukan koordinator global");

            await orchestrator.StartAsync(sagaId, new WaveCancelSaga(Guid.NewGuid(), "manual cancel"));
        }

        using var read = _provider.CreateScope();
        var context = read.ServiceProvider.GetRequiredService<OutboundDbContext>();
        var saga = await context.Set<SagaState>().AsNoTracking().FirstOrDefaultAsync(state => state.SagaId == sagaId);

        saga.Should().NotBeNull();
        saga!.Status.Should().Be(SagaState.StartedStatus);
        saga.SagaType.Should().Be(nameof(WaveCancelSaga));
        saga.State.Should().Contain("manual cancel");
    }

    private sealed record WaveCancelSaga(Guid WaveId, string Reason);
}
