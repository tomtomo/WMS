using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries.MarkDeliveryRead;

namespace Wms.Notifications.Endpoints;

// Endpoint inbox notifikasi untuk list, unread count, dan mark-read per user.
public sealed class NotificationInboxEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = NotificationsApiRoutes.Inbox(app);
        group.MapGet("/", ListAsync).WithName("GetInbox");
        group.MapGet("/summary", SummaryAsync).WithName("GetInboxSummary");
        group.MapPost("/{deliveryId:guid}/read", MarkReadAsync).WithName("MarkInboxItemRead");
    }

    private static async Task<IResult> ListAsync(
        INotificationInboxReader reader,
        ICurrentUser currentUser,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool unreadOnly = false)
    {
        var result = await reader.ListAsync(ResolveUserId(currentUser), page, pageSize, unreadOnly, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SummaryAsync(
        INotificationInboxReader reader,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var summary = await reader.GetSummaryAsync(ResolveUserId(currentUser), cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> MarkReadAsync(
        Guid deliveryId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkDeliveryReadCommand(deliveryId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static Guid ResolveUserId(ICurrentUser currentUser) =>
        Guid.TryParse(currentUser.UserId, out var userId) ? userId : Guid.Empty;
}
