namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Menyimpan correlation id dari request atau message yang sedang diproses.
// Nilainya bisa null saat tidak ada konteks correlation, misalnya saat worker baru start.
public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
