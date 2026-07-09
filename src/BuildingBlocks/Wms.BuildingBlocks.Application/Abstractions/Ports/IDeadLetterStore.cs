namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Simpan pesan yang gagal diproses
public interface IDeadLetterStore
{
    Task StoreAsync(string logicalName, string payload, string reason, int attemptCount, CancellationToken cancellationToken = default);
}
