using MediatR;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Behaviors;

// Pipeline Behavior — operational audit log, pipeline ke tiga.
public sealed class AuditLogBehavior<TRequest, TResponse>(
    IAuditLogStore auditLogStore,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (request is ICommandBase && response.IsSuccess)
        {
            var entry = new AuditLogEntry(currentUser.UserId, typeof(TRequest).Name, timeProvider.GetUtcNow());
            await auditLogStore.RecordAsync(entry, cancellationToken);
        }

        return response;
    }
}
