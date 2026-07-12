using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// Pastikan scan yang berhasil mengirim telemetry operasional berdasarkan operator dan gudang tanpa mengganggu proses utama.
[Collection(PostgresCollection.Name)]
public sealed class ScanTelemetryTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly Guid _operatorId = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

    private ServiceProvider _provider = null!;
    private CapturingEventStreamPublisher _stream = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InboundTestHost.Build(
            connectionString,
            services => services.AddSingleton<ICurrentUser>(new GuidCurrentUser(_operatorId)));
        await InboundTestHost.MigrateAsync(_provider);
        _stream = (CapturingEventStreamPublisher)_provider.GetRequiredService<IEventStreamPublisher>();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Scan_emits_scan_completed_telemetry_with_operator_and_warehouse()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_provider, ("SKU-A", 10m));

        await GoodsReceiptScenarios.ScanAsync(_provider, grId, "SKU-A", 8m, batch: "B1");

        var records = _stream.On<OperationalTelemetryRecord>(OperationalTelemetryStream.Name);
        records.Should().ContainSingle();
        var record = records[0];
        record.EventType.Should().Be(OperationalTelemetryEventType.ScanCompleted);
        record.WarehouseId.Should().Be(GoodsReceiptScenarios.WarehouseId);
        record.OperatorId.Should().Be(_operatorId);
        record.EntityId.Should().Be(grId);
        record.Quantity.Should().Be(8m);
    }

    [Fact]
    public async Task Scan_succeeds_even_when_stream_publisher_throws()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        await using var provider = InboundTestHost.Build(
            connectionString,
            services => services.AddSingleton<IEventStreamPublisher>(new ThrowingEventStreamPublisher()));
        await InboundTestHost.MigrateAsync(provider);
        var grId = await GoodsReceiptScenarios.CreateAsync(provider, ("SKU-A", 10m));

        // publisher stream down tak boleh menggagalkan scan
        await GoodsReceiptScenarios.ScanAsync(provider, grId, "SKU-A", 5m);
    }

    private sealed class ThrowingEventStreamPublisher : IEventStreamPublisher
    {
        public Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
            where TEvent : notnull => throw new InvalidOperationException("stream down");
    }

    // ICurrentUser dengan UserId berupa GUID, bukan "test-operator".
    private sealed class GuidCurrentUser(Guid userId) : ICurrentUser
    {
        public string UserId { get; } = userId.ToString();

        public bool IsAuthenticated => true;

        public bool CanBypassWarehouseScope => true;

        public bool HasPermission(string permission) => true;
    }
}
