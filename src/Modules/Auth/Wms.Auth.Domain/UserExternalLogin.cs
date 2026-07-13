using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

// Menghubungkan akun eksternal ke pengguna WMS secara manual tanpa membuat user baru otomatis.
public sealed class UserExternalLogin : AggregateRoot<UserExternalLoginId>, IAuditable
{
    private UserExternalLogin(UserExternalLoginId id, string provider, string subject, UserId userId)
        : base(id)
    {
        Provider = provider;
        Subject = subject;
        UserId = userId;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private UserExternalLogin()
        : base(default!)
    {
        Provider = null!;
        Subject = null!;
        UserId = null!;
    }

    // Provider identitas eksternal (mis. "entra").
    public string Provider { get; private set; }

    // Subject di provider (Entra: oid).
    public string Subject { get; private set; }

    public UserId UserId { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<UserExternalLogin> Link(
        UserExternalLoginId id,
        string provider,
        string subject,
        UserId userId)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(userId);

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result.Invalid<UserExternalLogin>(new Error("user_external_login.provider_required", "Provider wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Invalid<UserExternalLogin>(new Error("user_external_login.subject_required", "Subject wajib diisi."));
        }

        return Result.Success(new UserExternalLogin(id, provider.Trim(), subject.Trim(), userId));
    }
}
