using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Notifications.Deliveries;

// Delivery notifikasi untuk satu user.
public sealed class NotificationDelivery : AggregateRoot<DeliveryId>, IAuditable
{
    private NotificationDelivery(
        DeliveryId id,
        Guid? subscriptionId,
        Guid userId,
        Channel channel,
        string title,
        string body,
        string eventType,
        Guid? warehouseId,
        string eventRef)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        Channel = channel;
        Title = title;
        Body = body;
        EventType = eventType;
        WarehouseId = warehouseId;
        EventRef = eventRef;
        State = DeliveryState.Pending;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private NotificationDelivery()
        : base(default!)
    {
        Title = null!;
        Body = null!;
        EventType = null!;
        EventRef = null!;
    }

    // Null jika delivery dibuat langsung tanpa subscription.
    public Guid? SubscriptionId { get; private set; }

    public Guid UserId { get; private set; }

    public Channel Channel { get; private set; }

    // title/body = isi terrender handler.
    public string Title { get; private set; }

    public string Body { get; private set; }

    // Logical source event (mis. "WaveReady") — untuk audit dan grouping inbox.
    public string EventType { get; private set; }

    public Guid? WarehouseId { get; private set; }

    // Event yang menjadi sumber notifikasi.
    public string EventRef { get; private set; }

    public DeliveryState State { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public string? FailureReason { get; private set; }

    public int RetryCount { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<NotificationDelivery> Enqueue(
        DeliveryId id,
        Guid? subscriptionId,
        Guid userId,
        Channel channel,
        string title,
        string body,
        string eventType,
        Guid? warehouseId,
        string eventRef)
    {
        ArgumentNullException.ThrowIfNull(id);

        var error = Validate(userId, title, body, eventType, eventRef);
        if (error is not null)
        {
            return Result.Invalid<NotificationDelivery>(error);
        }

        return Result.Success(new NotificationDelivery(
            id, subscriptionId, userId, channel, title.Trim(), body.Trim(), eventType.Trim(), warehouseId, eventRef.Trim()));
    }

    // Jika sudah terkirim, pertahankan ProviderMessageId yang sudah ada.
    public Result MarkSent(string? providerMessageId)
    {
        if (State == DeliveryState.Sent)
        {
            return Result.Success();
        }

        if (State == DeliveryState.Read)
        {
            return Result.Conflict(new Error("delivery.cannot_send", "Delivery yang sudah dibaca tak bisa dikirim ulang."));
        }

        State = DeliveryState.Sent;
        ProviderMessageId = providerMessageId;
        FailureReason = null;
        return Result.Success();
    }

    public Result MarkFailed(string failureReason, int retryCount)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            return Result.Invalid(new Error("delivery.failure_reason_required", "Alasan kegagalan wajib diisi."));
        }

        if (State is DeliveryState.Sent or DeliveryState.Read)
        {
            return Result.Conflict(new Error("delivery.cannot_fail", "Delivery yang sudah terkirim tak bisa ditandai gagal."));
        }

        State = DeliveryState.Failed;
        FailureReason = failureReason.Trim();
        RetryCount = retryCount;
        return Result.Success();
    }

    // Read hanya applicable channel InApp
    public Result MarkRead()
    {
        if (Channel != Channel.InApp)
        {
            return Result.Failure(new Error("delivery.read_not_applicable", "Mark-as-read hanya berlaku untuk channel in-app."));
        }

        if (State == DeliveryState.Read)
        {
            return Result.Success();
        }

        State = DeliveryState.Read;
        return Result.Success();
    }

    private static Error? Validate(Guid userId, string title, string body, string eventType, string eventRef)
    {
        if (userId == Guid.Empty)
        {
            return new Error("delivery.user_required", "User penerima wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return new Error("delivery.title_required", "Judul notifikasi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new Error("delivery.body_required", "Isi notifikasi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return new Error("delivery.event_type_required", "Event type wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(eventRef))
        {
            return new Error("delivery.event_ref_required", "Referensi event wajib diisi.");
        }

        return null;
    }
}
