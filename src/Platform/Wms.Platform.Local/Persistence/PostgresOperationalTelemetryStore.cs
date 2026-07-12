using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Persistence;

// Simpan telemetry operasional Local di PostgreSQL tanpa masa simpan otomatis.
// Store ini memakai koneksi ke database bersama agar datanya dapat dibaca oleh Reporting.
public sealed class PostgresOperationalTelemetryStore : IOperationalTelemetryStore, IDisposable
{
    // Batasi pencarian maksimal 7 hari dan 5.000 data agar tetap setara dengan penyimpanan Cosmos.
    private const int MaxRows = 5000;

    private const string EnsureTableSql = """
        CREATE SCHEMA IF NOT EXISTS reporting;
        CREATE TABLE IF NOT EXISTS reporting.operational_telemetry (
            id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            occurred_at timestamptz NOT NULL,
            warehouse_id uuid NOT NULL,
            operator_id uuid NULL,
            event_type text NOT NULL,
            entity_id uuid NOT NULL,
            quantity numeric NULL);
        CREATE INDEX IF NOT EXISTS ix_operational_telemetry_warehouse_time
            ON reporting.operational_telemetry (warehouse_id, occurred_at DESC);
        """;

    private static readonly TimeSpan _maxWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan _defaultWindow = TimeSpan.FromHours(1);

    private readonly NpgsqlDataSource _dataSource;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _bootstrapLock = new(1, 1);
    private volatile bool _bootstrapped;

    public PostgresOperationalTelemetryStore(string connectionString, TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _dataSource = NpgsqlDataSource.Create(connectionString);
        _timeProvider = timeProvider;
    }

    public async Task AppendAsync(OperationalTelemetryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var command = _dataSource.CreateCommand("""
            INSERT INTO reporting.operational_telemetry
                (occurred_at, warehouse_id, operator_id, event_type, entity_id, quantity)
            VALUES (@occurredAt, @warehouseId, @operatorId, @eventType, @entityId, @quantity)
            """);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("occurredAt", record.OccurredAt);
            command.Parameters.AddWithValue("warehouseId", record.WarehouseId);
            command.Parameters.AddWithValue("operatorId", (object?)record.OperatorId ?? DBNull.Value);
            command.Parameters.AddWithValue("eventType", record.EventType.ToString());
            command.Parameters.AddWithValue("entityId", record.EntityId);
            command.Parameters.AddWithValue("quantity", (object?)record.Quantity ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<OperationalTelemetryRecord>> GetRecentAsync(
        Guid warehouseId,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
        var since = _timeProvider.GetUtcNow() - ClampWindow(window);

        var command = _dataSource.CreateCommand("""
            SELECT occurred_at, warehouse_id, operator_id, event_type, entity_id, quantity
            FROM reporting.operational_telemetry
            WHERE warehouse_id = @warehouseId AND occurred_at >= @since
            ORDER BY occurred_at DESC
            LIMIT @maxRows
            """);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("warehouseId", warehouseId);
            command.Parameters.AddWithValue("since", since);
            command.Parameters.AddWithValue("maxRows", MaxRows);

            var records = new List<OperationalTelemetryRecord>();
            var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    records.Add(new OperationalTelemetryRecord(
                        await reader.GetFieldValueAsync<DateTimeOffset>(0, cancellationToken).ConfigureAwait(false),
                        reader.GetGuid(1),
                        await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false) ? null : reader.GetGuid(2),
                        Enum.Parse<OperationalTelemetryEventType>(reader.GetString(3)),
                        reader.GetGuid(4),
                        await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetDecimal(5)));
                }
            }

            return records;
        }
    }

    public void Dispose() => _dataSource.Dispose();

    private static TimeSpan ClampWindow(TimeSpan window)
    {
        if (window <= TimeSpan.Zero)
        {
            return _defaultWindow;
        }

        return window > _maxWindow ? _maxWindow : window;
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_bootstrapped)
        {
            return;
        }

        await _bootstrapLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bootstrapped)
            {
                return;
            }

            var command = _dataSource.CreateCommand(EnsureTableSql);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            _bootstrapped = true;
        }
        finally
        {
            _bootstrapLock.Release();
        }
    }
}
