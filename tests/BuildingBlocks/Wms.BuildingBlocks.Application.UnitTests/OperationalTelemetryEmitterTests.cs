using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Contracts.Abstractions;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Pastikan telemetry operasional dikirim ke stream yang benar tanpa menggagalkan proses bisnis saat pengiriman bermasalah.
public sealed class OperationalTelemetryEmitterTests
{
    [Fact]
    public async Task Emits_record_to_operational_telemetry_stream()
    {
        var publisher = Substitute.For<IEventStreamPublisher>();
        var emitter = new OperationalTelemetryEmitter(publisher, NullLogger<OperationalTelemetryEmitter>.Instance);
        var record = SampleRecord();

        await emitter.EmitAsync(record, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            OperationalTelemetryStream.Name,
            record,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publisher_failure_does_not_propagate()
    {
        var publisher = Substitute.For<IEventStreamPublisher>();
        publisher
            .PublishAsync(Arg.Any<string>(), Arg.Any<OperationalTelemetryRecord>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("stream down")));
        var emitter = new OperationalTelemetryEmitter(publisher, NullLogger<OperationalTelemetryEmitter>.Instance);

        var act = async () => await emitter.EmitAsync(SampleRecord(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Warehouse_id_is_the_stream_partition_key()
    {
        var warehouseId = Guid.NewGuid();
        var record = SampleRecord() with { WarehouseId = warehouseId };

        // Partisi Event Hubs per gudang: publisher membaca IHasPartitionKey.
        ((IHasPartitionKey)record).PartitionKey.Should().Be(warehouseId.ToString());
    }

    [Fact]
    public void Record_round_trips_through_default_stj_event_hubs_wire_shape()
    {
        // Pastikan format JSON dari Event Hubs dapat dibaca kembali oleh trigger dengan konfigurasi System.Text.Json yang sama.
        var record = SampleRecord() with { OperatorId = Guid.NewGuid(), Quantity = 12.5m };

        var body = Encoding.UTF8.GetString(JsonSerializer.SerializeToUtf8Bytes(record, record.GetType()));
        var roundTripped = JsonSerializer.Deserialize<OperationalTelemetryRecord>(body);

        roundTripped.Should().Be(record);
    }

    private static OperationalTelemetryRecord SampleRecord() => new(
        DateTimeOffset.UnixEpoch,
        Guid.NewGuid(),
        Guid.NewGuid(),
        OperationalTelemetryEventType.ScanCompleted,
        Guid.NewGuid(),
        5m);
}
