using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.Features.Login;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.EntraLogin;

// Validasi token Entra, cari pengguna yang sudah terhubung, lalu terbitkan JWT internal melalui alur login yang sama.
internal sealed class EntraLoginHandler(
    IEntraTokenValidator entraTokenValidator,
    IUserExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    IJwtTokenIssuer jwtTokenIssuer,
    IEffectivePermissionResolver permissionResolver,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    IAuditLogStore auditLogStore,
    TimeProvider timeProvider)
    : ICommandHandler<EntraLoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(EntraLoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();

        var identity = await entraTokenValidator.ValidateAsync(command.IdToken, cancellationToken);
        if (identity.IsFailure)
        {
            return identity.ForwardFailure<LoginResponse>();
        }

        var subject = identity.Value.ObjectId;
        var userId = await externalLoginRepository.FindUserIdAsync(
            ExternalLoginProviders.Entra, subject, cancellationToken);

        // Tolak login jika akun Entra belum ditautkan ke pengguna WMS. Akun baru tidak dibuat otomatis.
        if (userId is null)
        {
            await auditLogStore.RecordAsync(
                new AuditLogEntry(identity.Value.UserPrincipalName ?? subject, "EntraLoginUnlinked", now),
                cancellationToken);
            return NotLinked();
        }

        var user = await userRepository.GetAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotLinked();
        }

        if (user.Status == UserStatus.Disabled)
        {
            return Result.Invalid<LoginResponse>(new Error("auth.user_disabled", "Akun dinonaktifkan."));
        }

        if (user.Status == UserStatus.Locked)
        {
            if (!user.IsLockoutExpired(now))
            {
                return Result.Conflict<LoginResponse>(
                    new Error("auth.user_locked", "Akun terkunci sementara akibat percobaan login gagal."));
            }

            user.Unlock();
        }

        user.RecordSuccessfulLogin();

        var permissions = await permissionResolver.ResolveAsync(user, cancellationToken);
        var accessToken = await jwtTokenIssuer.IssueAsync(user, permissions, cancellationToken);

        var material = refreshTokenFactory.Create();
        var refreshToken = RefreshToken.Issue(
            RefreshTokenId.Create(Guid.NewGuid()).Value,
            user.Id,
            material.TokenHash,
            now,
            now + AuthTokenDefaults._refreshTokenLifetime);
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Success(new LoginResponse(accessToken.Token, accessToken.ExpiresAt, material.RawToken));
    }

    private static Result<LoginResponse> NotLinked() =>
        Result.Invalid<LoginResponse>(
            new Error("auth.entra_not_linked", "Identitas Entra belum ditautkan ke user WMS."));
}
