namespace Wms.Platform.Azure.Persistence;

// Dokumen Cosmos untuk menyimpan projection beserta tipe, key, dan waktu terakhir diupdate.
// ID menggabungkan tipe dan key agar beberapa jenis projection dapat memakai container yang sama.
internal sealed class ProjectionDocument<TProjection>
{
    public string Id { get; set; } = string.Empty;

    // Gunakan key projection sebagai partition key karena data selalu dibaca langsung berdasarkan key.
    public string PartitionKey { get; set; } = string.Empty;

    public string ProjectionType { get; set; } = string.Empty;

    public TProjection Document { get; set; } = default!;

    public DateTimeOffset UpdatedAt { get; set; }
}
