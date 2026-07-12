using System.Globalization;
using AwesomeAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Azure.Persistence;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan mapping dokumen telemetry Cosmos benar dan data dapat disimpan lalu dibaca kembali pada resource test.
public sealed class CosmosOperationalTelemetryStoreTests
{
    private static readonly DateTimeOffset _epoch = new(2026, 7, 12, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Document_round_trips_the_record_and_ids_by_tick()
    {
        var record = new OperationalTelemetryRecord(
            _epoch, Guid.NewGuid(), Guid.NewGuid(), OperationalTelemetryEventType.PickCompleted, Guid.NewGuid(), 7m);

        var document = CosmosTelemetryDocument.From(record);

        document.ToRecord().Should().BeEquivalentTo(record);
        document.WarehouseId.Should().Be(record.WarehouseId, "partition key /warehouseId");
        document.Id.Should().StartWith(record.OccurredAt.UtcTicks.ToString(CultureInfo.InvariantCulture) + ":");
    }

    [Fact]
    public void Null_operator_and_quantity_survive_round_trip()
    {
        var record = new OperationalTelemetryRecord(
            _epoch, Guid.NewGuid(), OperatorId: null, OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), Quantity: null);

        CosmosTelemetryDocument.From(record).ToRecord().Should().BeEquivalentTo(record);
    }

    [Fact]
    public void Each_document_gets_a_distinct_id_even_at_the_same_tick()
    {
        var record = new OperationalTelemetryRecord(
            _epoch, Guid.NewGuid(), Guid.NewGuid(), OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), 1m);

        CosmosTelemetryDocument.From(record).Id.Should().NotBe(CosmosTelemetryDocument.From(record).Id);
    }

    [SkippableFact]
    public async Task Live_cosmos_append_then_read_recent_round_trips()
    {
        Skip.IfNot(AzureLiveSettings.HasCosmos, "WMS_PARITY_COSMOS_CONN tidak diset — lewati uji live Cosmos.");

        var options = new CosmosOptions { DatabaseName = "wms_parity", TelemetryContainerName = "operational-telemetry-parity" };
        using (var client = CosmosClientFactory.CreateWithConnectionString(AzureLiveSettings.CosmosConnectionString!, options))
        {
            var database = (await client.CreateDatabaseIfNotExistsAsync(options.DatabaseName).ConfigureAwait(false)).Database;
            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(options.TelemetryContainerName, "/warehouseId") { DefaultTimeToLive = 604800 })
                .ConfigureAwait(false);
            try
            {
                var store = new CosmosOperationalTelemetryStore(client, Options.Create(options), TimeProvider.System);
                var warehouseId = Guid.NewGuid();
                var record = new OperationalTelemetryRecord(
                    DateTimeOffset.UtcNow, warehouseId, Guid.NewGuid(), OperationalTelemetryEventType.ScanCompleted, Guid.NewGuid(), 9m);
                await store.AppendAsync(record);

                var recent = await store.GetRecentAsync(warehouseId, TimeSpan.FromHours(1));

                recent.Should().ContainSingle().Which.EntityId.Should().Be(record.EntityId);
            }
            finally
            {
                await database.GetContainer(options.TelemetryContainerName).DeleteContainerAsync().ConfigureAwait(false);
            }
        }
    }
}
