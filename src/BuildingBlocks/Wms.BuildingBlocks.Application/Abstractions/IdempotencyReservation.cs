namespace Wms.BuildingBlocks.Application.Abstractions;

public enum IdempotencyReservationStatus
{
    // Pemanggil memenangkan klaim dan wajib eksekusi lalu Complete/Release.
    Reserved,

    // Request lain masih memproses key yang sama.
    Pending,

    // Sudah ada respons tersimpan — replay tanpa eksekusi.
    Completed,
}

// OwnerToken memastikan hanya pemilik reservasi saat ini yang boleh Complete/Release.
public sealed record IdempotencyReservation(
    IdempotencyReservationStatus Status,
    string? StoredResponse,
    Guid OwnerToken)
{
    public static readonly IdempotencyReservation Pending = new(IdempotencyReservationStatus.Pending, null, Guid.Empty);

    public static IdempotencyReservation Reserved(Guid ownerToken) =>
        new(IdempotencyReservationStatus.Reserved, null, ownerToken);

    public static IdempotencyReservation Completed(string storedResponse) =>
        new(IdempotencyReservationStatus.Completed, storedResponse, Guid.Empty);
}
