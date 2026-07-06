using Wms.Auth.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.Logout;

internal sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tokenHash = refreshTokenFactory.Hash(command.RefreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        return stored is null
            ? Result.Success()
            : stored.Revoke(timeProvider.GetUtcNow());
    }
}
