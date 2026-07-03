using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Abstractions;

// Application commit tanpa tahu EF/DbContex.
public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
