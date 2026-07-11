using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wms.Auth.Grpc.V1;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Functions.Azure;
using Wms.Notifications.Persistence;
using Wms.Notifications.UserDirectory;
using Wms.Platform.Shared.Notifications;

// Function Notifications memakai modul, adapter Azure, dan rail dispatcher, sedangkan proses subscribe ditangani oleh trigger.
// DeliveryDispatcherWorker tetap berjalan selama host aktif dan ikut hidup kembali saat trigger mengaktifkan host.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationBuildingBlocks(typeof(NotificationsDbContext).Assembly);

        // Worker tidak memiliki HttpContext, jadi proses audit dijalankan sebagai SYSTEM.
        services.AddSystemCurrentUser();
        services.AddBuildingBlocksInfrastructure("wms-notifications");
        services.AddNotificationsModule(context.Configuration);

        // IHubContext dibutuhkan oleh channel notifikasi in app.
        // Worker tidak membuka endpoint SignalR, jadi message real time tidak memiliki client, tetapi inbox tetap tersimpan.
        services.AddSignalR();

        // Jika Firebase belum dikonfigurasi, gunakan client yang selalu gagal agar error pengiriman push tetap terlihat.
        var firebaseServiceAccountJson = context.Configuration["Firebase:ServiceAccountJson"];
        if (string.IsNullOrWhiteSpace(firebaseServiceAccountJson))
        {
            services.AddSingleton<IFirebaseMessagingClient, UnconfiguredFirebaseMessagingClient>();
        }
        else
        {
            // Service account berasal dari JSON utuh di Key Vault, bukan dari file lokal.
            // Warning dinonaktifkan hanya untuk pemanggilan FromJson ini.
#pragma warning disable CS0618
            services.AddSingleton(FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseServiceAccountJson),
            }));
#pragma warning restore CS0618
        }

        services.AddAzurePlatform(context.Configuration);

        // User directory memakai gRPC ke Auth untuk membaca user dan anggota role, dengan endpoint yang diinjeksi dari IaC.
        var authAddress = new Uri(
            context.Configuration["Services:Auth:Grpc"]
            ?? throw new InvalidOperationException("Konfigurasi 'Services:Auth:Grpc' wajib ada (di-inject IaC)."));
        services.AddInternalGrpcClient<AuthLookup.AuthLookupClient>(authAddress);
        services.AddScoped<IUserDirectory, AuthGrpcUserDirectory>();

        // Notifications hanya memproses event, sedangkan lifecycle message ditangani oleh EventGridTrigger.
        services.AddNotificationsRailConsumers();
        services.AddEventingRailDispatchOnly();
    })
    .Build();

await host.RunAsync();
