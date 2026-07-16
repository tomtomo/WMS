using MudBlazor;

namespace Wms.WebUI.Services;

// Helper tampilan untuk order dan wave agar ID, ringkasan isi, dan warna status konsisten di seluruh halaman outbound.
public static class OrderDisplay
{
    // Tampilkan 8 karakter awal ID agar lebih ringkas, sementara link tetap memakai GUID lengkap.
    public static string Short(Guid id) => id.ToString()[..8];

    // Ringkas isi order dari line pertama dan tampilkan jumlah line tambahan jika ada.
    public static string LinesSummary(IReadOnlyList<OutboundOrderLineDto> lines)
    {
        if (lines.Count == 0)
        {
            return "—";
        }

        var first = lines[0];
        var head = $"{first.Sku} ×{first.Qty:0.##}";
        return lines.Count == 1 ? head : $"{head} (+{lines.Count - 1})";
    }

    // Gunakan warna yang sama untuk status selesai, sedang berjalan, dan bermasalah.
    public static Color StatusColor(string status) => status switch
    {
        "Closed" or "Dispatched" => Color.Success,
        "InProgress" or "Active" or "Ready" or "New" => Color.Info,
        "Cancelled" or "Hold" or "Short" => Color.Error,
        _ => Color.Default,
    };
}
