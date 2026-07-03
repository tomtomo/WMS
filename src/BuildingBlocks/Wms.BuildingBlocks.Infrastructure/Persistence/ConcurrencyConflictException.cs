namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Translate dari DbUpdateConcurrencyException agar Application tak pernah menyentuh tipe EF.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
