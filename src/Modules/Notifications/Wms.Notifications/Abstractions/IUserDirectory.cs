namespace Wms.Notifications.Abstractions;

// Menyediakan data penerima notifikasi.
public interface IUserDirectory
{
    Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken = default);
}
