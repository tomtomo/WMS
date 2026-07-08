using System.Reflection;
using MediatR;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Application.Behaviors;

// Mengecek permission untuk request yang memakai RequiresPermission.
public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    // Permission yang dibutuhkan request ini.
    private static readonly string? _requiredPermission =
        typeof(TRequest).GetCustomAttribute<RequiresPermissionAttribute>()?.Permission;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Request tanpa permission, atau actor sistem, langsung lanjut.
        if (_requiredPermission is null || !currentUser.IsAuthenticated)
        {
            return await next(cancellationToken);
        }

        if (currentUser.HasPermission(_requiredPermission))
        {
            return await next(cancellationToken);
        }

        return PipelineFailure.Create<TResponse>(
            ResultErrorType.Forbidden,
            new Error("authorization.forbidden", $"Permission '{_requiredPermission}' diperlukan."));
    }
}
