using MediatR;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Messaging;

// Query/Read dengan Result<TResult>. Tidak lewat TransactionBehavior.
public interface IQuery<TResult> : IRequest<Result<TResult>>
{
}

public interface IQueryHandler<TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>
{
}
