using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Abstractions;

// Read port Dispatch Summary
public interface IDispatchSummaryReader : IReader
{
    Task<PagedResult<DispatchSummaryRow>> ListAsync(
        Guid? warehouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
