using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.RefreshAccessToken;

internal sealed class RefreshAccessTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRefreshTokenFactory refreshTokenFactory,
    IJwtTokenIssuer jwtTokenIssuer,
    IEffectivePermissionResolver permissionResolver,
    IUnitOfWork unitOfWork,
    IAuditLogStore auditLogStore,
    TimeProvider timeProvider)
    : ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenResponse>
{
    public async Task<Result<RefreshAccessTokenResponse>> Handle(
        RefreshAccessTokenCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var tokenHash = refreshTokenFactory.Hash(command.RefreshToken);
        var stored = await refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            return Result.Invalid<RefreshAccessTokenResponse>(
                new Error("auth.refresh_token_invalid", "Refresh token tidak dikenal."));
        }

        // Token yang sudah pernah dicabut, anggap sebagai reuse, cabut seluruh rotation chain dan tolak request.
        if (stored.RevokedAt is not null)
        {
            await RevokeRotationChainAsync(stored, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Reuse refresh token tetap dicatat karena mencabut seluruh rotation chain,
            // sementara AuditLogBehavior hanya mencatat flow yang berhasil.
            await auditLogStore.RecordAsync(
                new AuditLogEntry(stored.UserId.Value.ToString(), "RefreshTokenReuseDetected", now), cancellationToken);
            return Result.Conflict<RefreshAccessTokenResponse>(
                new Error("auth.refresh_token_reused", "Refresh token sudah dipakai; seluruh sesi dicabut."));
        }

        if (!stored.IsActive(now))
        {
            return Result.Invalid<RefreshAccessTokenResponse>(
                new Error("auth.refresh_token_expired", "Refresh token kedaluwarsa."));
        }

        var user = await userRepository.GetAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Invalid<RefreshAccessTokenResponse>(
                new Error("auth.user_inactive", "User pemilik token tidak aktif."));
        }

        // Cabut token lama dan buat token pengganti
        var newTokenId = RefreshTokenId.Create(Guid.NewGuid()).Value;
        var rotate = stored.Rotate(newTokenId, now);
        if (rotate.IsFailure)
        {
            return rotate.ForwardFailure<RefreshAccessTokenResponse>();
        }

        var material = refreshTokenFactory.Create();
        var rotated = RefreshToken.Issue(newTokenId, user.Id, material.TokenHash, now, now + AuthTokenDefaults._refreshTokenLifetime);
        await refreshTokenRepository.AddAsync(rotated, cancellationToken);

        var permissions = await permissionResolver.ResolveAsync(user, cancellationToken);
        var accessToken = await jwtTokenIssuer.IssueAsync(user, permissions, cancellationToken);

        return Result.Success(new RefreshAccessTokenResponse(accessToken.Token, accessToken.ExpiresAt, material.RawToken));
    }

    // Cabut seluruh rotation chain untuk mencegah penggunaan ulang refresh token.
    private async Task RevokeRotationChainAsync(RefreshToken start, DateTimeOffset now, CancellationToken cancellationToken)
    {
        RefreshToken? current = start;
        while (current is not null)
        {
            current.Revoke(now);

            if (current.ReplacedByTokenId is null)
            {
                break;
            }

            current = await refreshTokenRepository.GetAsync(current.ReplacedByTokenId, cancellationToken);
        }
    }
}
