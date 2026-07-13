using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Notifications;

// Daftarkan SignalR dan notifier untuk mengirim notifikasi langsung melalui hub.
public static class NotificationHubExtensions
{
    public const string HubPath = "/hubs/notifications";

    // Panggil sebelum konfigurasi platform agar notifier SignalR tidak digantikan oleh fallback. Nonaktifkan delivery lokal jika dijalankan oleh worker terpisah.
    public static IServiceCollection AddNotificationHub(this IServiceCollection services, bool coLocateDelivery = true)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();

        if (coLocateDelivery)
        {
            services.AddSingleton<IInAppNotifier, SignalRInAppNotifier>();
        }

        return services;
    }

    public static void MapNotificationHub(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHub<NotificationHub>(HubPath).RequireAuthorization();
}

// Gunakan klaim sub sebagai ID pengguna untuk pengiriman notifikasi melalui SignalR.
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(WmsClaimTypes.Subject)?.Value;
}
