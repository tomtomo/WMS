using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wms.Platform.Azure.Persistence;

// Change feed Cosmos memproses perubahan projection, sedangkan lease container menyimpan posisi terakhir.
// Beberapa instance host dapat berbagi proses tanpa menangani dokumen yang sama dua kali.
public sealed class CosmosChangeFeedProcessor : IHostedService
{
    private readonly CosmosOptions _options;
    private readonly IProjectionChangeHandler _handler;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CosmosChangeFeedProcessor> _logger;
    private readonly ChangeFeedProcessor _processor;

    public CosmosChangeFeedProcessor(
        CosmosClient client,
        IOptions<CosmosOptions> options,
        IProjectionChangeHandler handler,
        TimeProvider timeProvider,
        ILogger<CosmosChangeFeedProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _handler = handler;
        _timeProvider = timeProvider;
        _logger = logger;

        var monitored = client.GetContainer(_options.DatabaseName, _options.ProjectionContainerName);
        var leases = client.GetContainer(_options.DatabaseName, _options.LeaseContainerName);

        _processor = monitored
            .GetChangeFeedProcessorBuilder<ProjectionDocument<JsonElement>>(_options.ChangeFeedProcessorName, OnChangesAsync)
            .WithInstanceName(_options.ChangeFeedInstanceName)
            .WithLeaseContainer(leases)

            // Mulai dari waktu sekarang karena change feed ini hanya memproses perubahan baru, bukan membaca ulang dari awal.
            .WithStartTime(timeProvider.GetUtcNow().UtcDateTime)
            .WithStartTime(timeProvider.GetUtcNow().UtcDateTime)
            .WithPollInterval(_options.ChangeFeedPollInterval)
            .Build();
    }

    public Task StartAsync(CancellationToken cancellationToken) => _processor.StartAsync();

    public Task StopAsync(CancellationToken cancellationToken) => _processor.StopAsync();

    internal async Task OnChangesAsync(
        IReadOnlyCollection<ProjectionDocument<JsonElement>> documents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documents);

        // Abaikan batch kosong karena tidak ada data yang bisa diproses
        if (documents.Count == 0)
        {
            return;
        }

        var readAt = _timeProvider.GetUtcNow();
        var changes = documents
            .Select(document => new ProjectionChange(
                document.ProjectionType,
                document.PartitionKey,
                document.UpdatedAt,
                readAt - document.UpdatedAt))
            .ToList();

        var worstLag = changes.Max(change => change.Lag);
        if (worstLag > _options.ChangeFeedLagThreshold)
        {
            _logger.LogWarning(
                "Change feed projection tertinggal {LagSeconds:F1}s melewati ambang {ThresholdSeconds:F1}s",
                worstLag.TotalSeconds,
                _options.ChangeFeedLagThreshold.TotalSeconds);
        }

        await _handler.HandleAsync(changes, cancellationToken).ConfigureAwait(false);
    }
}
