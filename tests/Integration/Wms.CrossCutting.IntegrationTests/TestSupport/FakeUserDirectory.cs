using Wms.Notifications.Abstractions;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

internal sealed class FakeUserDirectory : IUserDirectory
{
    public Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<NotificationRecipient?>(new NotificationRecipient(userId, $"{userId:N}@wms.local", $"device-{userId:N}"));
}
