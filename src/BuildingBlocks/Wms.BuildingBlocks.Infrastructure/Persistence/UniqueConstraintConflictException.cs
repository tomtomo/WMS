namespace Wms.BuildingBlocks.Infrastructure.Persistence;

// Dipakai untuk menerjemahkan unique violation dari Postgres, supaya layer Application tidak perlu bergantung ke tipe EF atau Npgsql.
// Berbeda dari concurrency conflict: duplikat natural key bukan kasus lost update.
public sealed class UniqueConstraintConflictException : Exception
{
    public UniqueConstraintConflictException()
    {
    }

    public UniqueConstraintConflictException(string message)
        : base(message)
    {
    }

    public UniqueConstraintConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
