using Npgsql;

namespace Wms.WebUI.E2ETests;

// Baca outbox tiap modul untuk menyusun kembali urutan event sesuai charter §3.1.
// Status akhir di database saja belum cukup untuk membuktikan urutan 11 event pada kriteria 7.
public sealed class EventLogReader(WmsE2EProfile profile)
{
    // Batasi query ke event setelah test dimulai agar hasil run lama di persistent volume tidak ikut terbaca.
    public const string Sql =
        "SELECT logical_name, occurred_at, traceparent::text, payload::text " +
        "FROM infrastructure.outbox WHERE occurred_at >= @since ORDER BY occurred_at";

    // Outbox untuk alur ini tersebar di database Inbound, Inventory, dan Outbound.
    private static readonly string[] Modules = ["wms_inbound", "wms_inventory", "wms_outbound"];

    public async Task<IReadOnlyList<EventRow>> ReadSpineEventsAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var rows = new List<EventRow>();
        foreach (var module in Modules)
        {
            var connString = profile.DbFor(module);
            if (connString is null)
            {
                continue;
            }

            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(Sql, connection);
            command.Parameters.AddWithValue("since", since);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var traceparent = await reader.IsDBNullAsync(2, cancellationToken)
                    ? null
                    : await reader.GetFieldValueAsync<string>(2, cancellationToken);
                rows.Add(new EventRow(
                    module,
                    await reader.GetFieldValueAsync<string>(0, cancellationToken),
                    await reader.GetFieldValueAsync<DateTimeOffset>(1, cancellationToken),
                    traceparent,
                    await reader.GetFieldValueAsync<string>(3, cancellationToken)));
            }
        }

        return [.. rows.OrderBy(row => row.OccurredAt)];
    }
}

// Representasi satu event dari infrastructure.outbox.
public sealed record EventRow(string Module, string LogicalName, DateTimeOffset OccurredAt, string? Traceparent, string Payload);
