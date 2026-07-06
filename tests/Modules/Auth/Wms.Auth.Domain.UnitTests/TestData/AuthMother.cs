using Wms.Auth.Domain;
using Wms.Auth.Domain.ValueObjects;

namespace Wms.Auth.Domain.UnitTests.TestData;

// Aggregate Auth valid untuk behavior test.
internal static class AuthMother
{
    public static UserId NewUserId() => UserId.Create(Guid.NewGuid()).Value;

    public static RoleId NewRoleId() => RoleId.Create(Guid.NewGuid()).Value;

    public static PermissionId NewPermissionId() => PermissionId.Create(Guid.NewGuid()).Value;

    public static RefreshTokenId NewRefreshTokenId() => RefreshTokenId.Create(Guid.NewGuid()).Value;

    public static PermissionCode CodeOf(string code = "Inbound.PostGR") => PermissionCode.Create(code).Value;

    public static User AUser(
        string username = "operator1",
        string email = "operator1@wms.local",
        string passwordHash = "$argon2id$stub",
        IEnumerable<Guid>? roleIds = null,
        IEnumerable<Guid>? assignedWarehouseIds = null)
        => User.Create(NewUserId(), username, email, passwordHash, roleIds ?? [], assignedWarehouseIds ?? []).Value;

    public static Role ARole(string code = "OPERATOR", string name = "Operator", IEnumerable<Guid>? permissionIds = null)
        => Role.Create(NewRoleId(), code, name, permissionIds ?? []).Value;

    public static Permission APermission(string code = "Inbound.PostGR", string description = "Post goods receipt")
        => Permission.Create(NewPermissionId(), CodeOf(code), description).Value;

    public static RefreshToken ARefreshToken(
        UserId? userId = null,
        string tokenHash = "token-hash",
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? expiresAt = null)
    {
        var issued = issuedAt ?? DateTimeOffset.UnixEpoch;
        return RefreshToken.Issue(NewRefreshTokenId(), userId ?? NewUserId(), tokenHash, issued, expiresAt ?? issued.AddDays(7));
    }
}
