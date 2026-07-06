using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

// Refresh token yang disimpan dalam bentuk hash.
public sealed class RefreshToken : AggregateRoot<RefreshTokenId>, IAuditable
{
    private RefreshToken(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private RefreshToken()
        : base(default!)
    {
        UserId = null!;
        TokenHash = null!;
    }

    public UserId UserId { get; private set; }

    // Token disimpan dalam bentuk hash.
    public string TokenHash { get; private set; }

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    // ID refresh token pengganti setelah proses rotasi.
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // ID dibuat oleh handler.
    public static RefreshToken Issue(
        RefreshTokenId id,
        UserId userId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken(id, userId, tokenHash, issuedAt, expiresAt);
    }

    // Token aktif jika belum dicabut dan belum kedaluwarsa.
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    // Cabut token dan tandai token penggantinya.
    public Result Rotate(RefreshTokenId newTokenId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newTokenId);

        if (RevokedAt is not null)
        {
            return Result.Conflict(new Error("refresh_token.already_revoked", "Refresh token sudah dicabut."));
        }

        RevokedAt = now;
        ReplacedByTokenId = newTokenId;
        return Result.Success();
    }

    // Cabut refresh token.
    public Result Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
        return Result.Success();
    }
}
