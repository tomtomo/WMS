using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Saga;

// Adapter saga untuk Azure: Durable Task dipakai menjadwalkan orchestration di worker host.
public sealed class DurableFunctionsSagaOrchestrator(DurableTaskClient client) : ISagaOrchestrator
{
    public async Task StartAsync<TSagaData>(string sagaId, TSagaData data, CancellationToken cancellationToken = default)
        where TSagaData : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);
        ArgumentNullException.ThrowIfNull(data);

        // Agar konsisten dengan adapter in process: saga yang masih jalan tidak boleh dimulai lagi.
        var existing = await client.GetInstanceAsync(sagaId, cancellationToken).ConfigureAwait(false);
        if (existing is { IsCompleted: false })
        {
            throw new InvalidOperationException($"Saga '{sagaId}' sudah dimulai — start ganda ditolak.");
        }

        // Nama orchestration diambil dari tipe data saga. modul pemiliknya harus mendaftarkan orchestrator dengan nama yang sama.
        await client.ScheduleNewOrchestrationInstanceAsync(
            new TaskName(typeof(TSagaData).Name),
            data,
            new StartOrchestrationOptions(sagaId),
            cancellationToken).ConfigureAwait(false);
    }
}
