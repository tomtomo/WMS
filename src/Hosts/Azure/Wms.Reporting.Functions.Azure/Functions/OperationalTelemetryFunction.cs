using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Reporting.Functions.Azure;

// Trigger ini menerima batch telemetry operasional dari Event Hubs lalu menyimpannya ke Cosmos.
// Data boleh duplikat karena disimpan append only dan tidak memakai inbox.
public sealed class OperationalTelemetryFunction(
    IOperationalTelemetryStore store,
    ILogger<OperationalTelemetryFunction> logger)
{
    [Function("OperationalTelemetry")]
    public async Task RunAsync(
        [EventHubTrigger("wms-operational-telemetry", ConsumerGroup = "reporting-functions", Connection = "EventHubs")]
        string[] events,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var body in events)
        {
            // Gunakan format JSON bawaan yang sama dengan EventHubsEventStreamPublisher, bukan format MessageEnvelope.
            var record = JsonSerializer.Deserialize<OperationalTelemetryRecord>(body);
            if (record is null)
            {
                logger.LogWarning("Event telemetry operasional kosong/tak ter-deserialize, dilewati.");
                continue;
            }

            await store.AppendAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }
}
