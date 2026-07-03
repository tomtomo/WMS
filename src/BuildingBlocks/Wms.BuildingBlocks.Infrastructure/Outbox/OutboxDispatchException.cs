namespace Wms.BuildingBlocks.Infrastructure.Outbox;

// Dilempar saat publish satu envelope Outbox gagal, baris tetap unprocessed agar poll berikutnya mencoba lagi.
public sealed class OutboxDispatchException : Exception
{
    public OutboxDispatchException()
    {
    }

    public OutboxDispatchException(string message)
        : base(message)
    {
    }

    public OutboxDispatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
