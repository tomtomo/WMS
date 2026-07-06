using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Subscriptions;

// Aturan kapan & gimana user dapat notifikasi.
public sealed class NotificationSubscription : AggregateRoot<SubscriptionId>, IAuditable
{
    private readonly List<Channel> _channels = [];

    private NotificationSubscription(
        SubscriptionId id,
        SubscriberType subscriberType,
        Guid subscriberId,
        string eventType,
        IReadOnlyList<Channel> channels,
        Guid? warehouseScope)
        : base(id)
    {
        SubscriberType = subscriberType;
        SubscriberId = subscriberId;
        EventType = eventType;
        _channels.AddRange(channels);
        WarehouseScope = warehouseScope;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private NotificationSubscription()
        : base(default!)
    {
        EventType = null!;
    }

    public SubscriberType SubscriberType { get; private set; }

    // userId atau roleId sesuai SubscriberType.
    public Guid SubscriberId { get; private set; }

    // Integration event yang disubscribe
    public string EventType { get; private set; }

    public IReadOnlyList<Channel> Channels => _channels;

    // Filter opsional warehouse
    public Guid? WarehouseScope { get; private set; }

    public bool IsActive { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<NotificationSubscription> Create(
        SubscriptionId id,
        SubscriberType subscriberType,
        Guid subscriberId,
        string eventType,
        IReadOnlyList<Channel> channels,
        Guid? warehouseScope)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(channels);

        var error = Validate(subscriberId, eventType, channels);
        if (error is not null)
        {
            return Result.Invalid<NotificationSubscription>(error);
        }

        return Result.Success(new NotificationSubscription(
            id, subscriberType, subscriberId, eventType.Trim(), channels, warehouseScope));
    }

    public Result Update(IReadOnlyList<Channel> channels, Guid? warehouseScope)
    {
        ArgumentNullException.ThrowIfNull(channels);

        if (channels.Count == 0)
        {
            return Result.Invalid(new Error("subscription.channels_required", "Minimal satu channel wajib dipilih."));
        }

        _channels.Clear();
        _channels.AddRange(channels);
        WarehouseScope = warehouseScope;
        return Result.Success();
    }

    // Soft delete, idempotent.
    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

    private static Error? Validate(Guid subscriberId, string eventType, IReadOnlyList<Channel> channels)
    {
        if (subscriberId == Guid.Empty)
        {
            return new Error("subscription.subscriber_required", "Subscriber wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return new Error("subscription.event_type_required", "Event type wajib diisi.");
        }

        if (channels.Count == 0)
        {
            return new Error("subscription.channels_required", "Minimal satu channel wajib dipilih.");
        }

        return null;
    }
}
