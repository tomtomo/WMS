using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Inbound.Application.Abstractions;

// Port lookup Product milik Inbound.
public interface IProductReader : IReader
{
    Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default);
}
