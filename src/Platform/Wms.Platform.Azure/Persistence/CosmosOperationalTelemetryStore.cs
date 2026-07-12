using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Persistence;

// Simpan telemetry operasional Azure di Cosmos dengan masa simpan 7 hari.
// Data dipartisi berdasarkan warehouseId agar query per gudang tetap efisien.
public sealed class CosmosOperationalTelemetryStore : IOperationalTelemetryStore
{
    private const int MaxRows = 5000;

    private static readonly TimeSpan _maxWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan _defaultWindow = TimeSpan.FromHours(1);

    private readonly Container _container;
    private readonly TimeProvider _timeProvider;

    public CosmosOperationalTelemetryStore(CosmosClient client, IOptions<CosmosOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        _container = client.GetContainer(options.Value.DatabaseName, options.Value.TelemetryContainerName);
        _timeProvider = timeProvider;
    }

    public async Task AppendAsync(OperationalTelemetryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        var document = CosmosTelemetryDocument.From(record);
        await _container.CreateItemAsync(
            document,
            new PartitionKey(record.WarehouseId.ToString()),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OperationalTelemetryRecord>> GetRecentAsync(
        Guid warehouseId,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var since = _timeProvider.GetUtcNow() - ClampWindow(window);

        // Batasi hasil langsung di query agar jumlah data yang dibaca tidak melewati MaxRows.
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.warehouseId = @warehouseId AND c.occurredAt >= @since ORDER BY c.occurredAt DESC OFFSET 0 LIMIT @maxRows")
            .WithParameter("@warehouseId", warehouseId)
            .WithParameter("@since", since)
            .WithParameter("@maxRows", MaxRows);

        var records = new List<OperationalTelemetryRecord>();
        using var iterator = _container.GetItemQueryIterator<CosmosTelemetryDocument>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(warehouseId.ToString()) });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var document in page)
            {
                records.Add(document.ToRecord());
            }
        }

        return records;
    }

    private static TimeSpan ClampWindow(TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            return _defaultWindow;
        }

        return window > _maxWindow ? _maxWindow : window;
    }
}
