namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Notif push: FCM lintas cloud, log-stub Local.
public interface IPushNotifier
{
    Task PushAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default);
}
