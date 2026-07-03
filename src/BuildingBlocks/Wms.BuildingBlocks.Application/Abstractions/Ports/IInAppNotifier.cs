namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Notif in app: self hosted SignalR.
public interface IInAppNotifier
{
    Task NotifyAsync(string userId, string message, CancellationToken cancellationToken = default);
}
