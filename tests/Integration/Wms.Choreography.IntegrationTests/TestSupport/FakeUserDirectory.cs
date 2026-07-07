using Wms.Notifications.Abstractions;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Implement IUserDirectory
internal sealed class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _roleMembers = [];

    public void SetRoleMembers(Guid roleId, params Guid[] members) => _roleMembers[roleId] = members;

    public Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roleMembers.TryGetValue(roleId, out var members) ? members : []);

    public Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<NotificationRecipient?>(new NotificationRecipient(userId, $"{userId:N}@wms.local", $"device-{userId:N}"));
}
