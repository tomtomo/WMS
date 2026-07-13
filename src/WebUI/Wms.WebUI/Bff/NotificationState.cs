namespace Wms.WebUI.Bff;

// Simpan jumlah notifikasi yang belum dibaca dan beri tahu UI saat nilainya berubah.
public sealed class NotificationState
{
    public event Action? Changed;

    public int UnreadCount { get; private set; }

    public void Increment()
    {
        UnreadCount++;
        Changed?.Invoke();
    }

    public void Reset()
    {
        UnreadCount = 0;
        Changed?.Invoke();
    }
}
