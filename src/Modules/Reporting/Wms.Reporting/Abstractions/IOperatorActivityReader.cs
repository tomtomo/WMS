using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Abstractions;

// Read port Operator Productivity
public interface IOperatorActivityReader : IReader
{
    Task<PagedResult<OperatorProductivityRow>> ListAsync(
        Guid? operatorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
