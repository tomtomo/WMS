using MediatR;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Messaging;

// Command tanpa nilai balik Result. Alias di atas IRequest MediatR.
public interface ICommand : ICommandBase, IRequest<Result>
{
}

// Command bernilai balik Result<TResult>.
public interface ICommand<TResult> : ICommandBase, IRequest<Result<TResult>>
{
}

// Marker command — dibaca TransactionBehavior: hanya command yang commit, query tidak.
public interface ICommandBase
{
}

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>
{
}
