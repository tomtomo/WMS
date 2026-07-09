using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.Login;

internal sealed class LoginHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenIssuer jwtTokenIssuer,
    IEffectivePermissionResolver permissionResolver,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    IUnitOfWork unitOfWork,
    IAuditLogStore auditLogStore,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var user = await userRepository.GetByUsernameAsync(command.Username, cancellationToken);

        // User tidak ditemukan / password salah
        if (user is null)
        {
            return InvalidCredentials();
        }

        // Disabled vs Locked
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

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Login gagal tetap dicatat karena bisa mengubah status lockout,
            // sementara AuditLogBehavior hanya mencatat flow yang berhasil.
            await auditLogStore.RecordAsync(new AuditLogEntry(command.Username, "LoginFailed", now), cancellationToken);
            return InvalidCredentials();
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

    private static Result<LoginResponse> InvalidCredentials() =>
        Result.Invalid<LoginResponse>(new Error("auth.invalid_credentials", "Username atau password salah."));
}
