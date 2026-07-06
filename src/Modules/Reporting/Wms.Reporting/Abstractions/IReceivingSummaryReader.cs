using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Abstractions;

// Read port Supplier Performance
public interface IReceivingSummaryReader : IReader
{
    Task<PagedResult<SupplierPerformanceRow>> ListAsync(
        Guid? supplierId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
