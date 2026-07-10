namespace Wms.Contracts.Abstractions;

// Kontrak bisa menentukan key untuk menjaga urutan pengiriman event. Biasanya diisi eksplisit di record agar valuenya tidak ikut masuk ke payload.
public interface IHasPartitionKey
{
    string PartitionKey { get; }
}
