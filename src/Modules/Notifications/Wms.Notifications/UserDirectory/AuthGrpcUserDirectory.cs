using Grpc.Core;
using Wms.Auth.Grpc.V1;
using Wms.Notifications.Abstractions;

namespace Wms.Notifications.UserDirectory;

// User directory lewat gRPC ke Auth, dipakai untuk membaca anggota role dan data recipient.
public sealed class AuthGrpcUserDirectory(AuthLookup.AuthLookupClient client) : IUserDirectory
{
    public async Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var response = await client.GetRoleMembersAsync(
            new GetRoleMembersRequest { RoleId = roleId.ToString() }, cancellationToken: cancellationToken);
        return [.. response.UserIds.Select(Guid.Parse)];
    }

    public async Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await client.GetUserAsync(
                new GetUserRequest { UserId = userId.ToString() }, cancellationToken: cancellationToken);

            // Auth belum punya registry device, jadi token dibuat stabil dari user id. Di cloud nanti bagian ini diganti dengan data FCM atau APNs.
            return new NotificationRecipient(userId, user.Email, $"device-{userId:N}");
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}
