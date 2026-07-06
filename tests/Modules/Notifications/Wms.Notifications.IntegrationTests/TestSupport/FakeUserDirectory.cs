using Wms.Notifications.Abstractions;

namespace Wms.Notifications.IntegrationTests.TestSupport;

// IUserDirectory untuk kebutuhan test
public sealed class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _roleMembers = [];
    private readonly HashSet<Guid> _noRecipient = [];

    public void SetRoleMembers(Guid roleId, params Guid[] members) => _roleMembers[roleId] = members;

    // Tandai user yang tidak memiliki data recipient.
    public void SetNoRecipient(Guid userId) => _noRecipient.Add(userId);

    public Task<IReadOnlyList<Guid>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roleMembers.TryGetValue(roleId, out var members) ? members : []);

    public Task<NotificationRecipient?> GetRecipientAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<NotificationRecipient?>(_noRecipient.Contains(userId)
            ? null
            : new NotificationRecipient(userId, $"{userId:N}@wms.local", $"device-{userId:N}"));
}
