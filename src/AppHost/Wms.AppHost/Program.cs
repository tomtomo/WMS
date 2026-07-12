using System.Security.Cryptography;

// Titik masuk AppHost untuk local development. Sekali Run, semua resource lokal yang dibutuhkan ikut naik:
// Postgres per modul, RabbitMQ, migration runner, host modul, gateway, WebUI, dan dashboard OTLP.
const string JwtIssuer = "wms-local";
const string JwtAudience = "wms-local";

var builder = DistributedApplication.CreateBuilder(args);

// Keypair JWT untuk mode dev. Auth pakai private key buat issue token,
// sedangkan host lain dan gateway cukup pakai public key untuk validasi. (cloud: Key Vault)
using var jwtRsa = RSA.Create(2048);
var jwtPrivatePem = jwtRsa.ExportPkcs8PrivateKeyPem();
var jwtPublicPem = jwtRsa.ExportSubjectPublicKeyInfoPem();

// Satu server Postgres, tapi database dipisah per modul.
// Schema infrastructure.* tetap aman di masing-masing DB tanpa bentrok nama tabel.
var postgres = builder.AddPostgres("postgres").WithDataVolume();
var inboundDb = postgres.AddDatabase("inbounddb", "wms_inbound");
var inventoryDb = postgres.AddDatabase("inventorydb", "wms_inventory");
var outboundDb = postgres.AddDatabase("outbounddb", "wms_outbound");
var masterDataDb = postgres.AddDatabase("masterdatadb", "wms_masterdata");
var authDb = postgres.AddDatabase("authdb", "wms_auth");
var reportingDb = postgres.AddDatabase("reportingdb", "wms_reporting");
var notificationsDb = postgres.AddDatabase("notificationsdb", "wms_notifications");

var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

// Jalankan migration semua modul sekali sebelum host aplikasi mulai.
builder.AddProject<Projects.Wms_MigrationRunner>("migrations")
    .WithReference(inboundDb).WaitFor(inboundDb)
    .WithReference(inventoryDb).WaitFor(inventoryDb)
    .WithReference(outboundDb).WaitFor(outboundDb)
    .WithReference(masterDataDb).WaitFor(masterDataDb)
    .WithReference(authDb).WaitFor(authDb)
    .WithReference(reportingDb).WaitFor(reportingDb)
    .WithReference(notificationsDb).WaitFor(notificationsDb);

// Auth host: issue JWT dengan private key dan expose gRPC AuthLookup untuk Notifications
var auth = WithJwtValidation(builder.AddProject<Projects.Wms_Auth_Host_Local>("wms-auth"))
    .WithReference(authDb, "wms").WaitFor(authDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Secrets__jwt-signing-key", jwtPrivatePem);

// MasterData host: expose gRPC MasterDataLookup untuk dipakai modul core.
var masterData = WithActiveUserChecker(WithJwtValidation(builder.AddProject<Projects.Wms_MasterData_Host_Local>("wms-masterdata")))
    .WithReference(masterDataDb, "wms").WaitFor(masterDataDb)
    .WithReference(rabbitmq);

// Core host memakai MasterData via gRPC. Endpoint diinjek langsung dari AppHost.
var inbound = WithActiveUserChecker(WithJwtValidation(builder.AddProject<Projects.Wms_Inbound_Host_Local>("wms-inbound")))
    .WithReference(inboundDb, "wms").WaitFor(inboundDb)
    .WithReference(rabbitmq)
    .WithReference(masterData)
    .WithEnvironment("Services__MasterData__Grpc", masterData.GetEndpoint("https"));

// Base URL object store lokal diarahkan ke host Inbound sendiri.
// Browser fetch langsung ke /files, lalu signature HMAC divalidasi terhadap scheme, host, dan path.
inbound.WithEnvironment(
    "LocalPlatform__ObjectStore__BaseUrl",
    ReferenceExpression.Create($"{inbound.GetEndpoint("https")}/files"));

// Lokasi receiving/picking memakai GUID seed
var inventory = WithActiveUserChecker(WithJwtValidation(builder.AddProject<Projects.Wms_Inventory_Host_Local>("wms-inventory")))
    .WithReference(inventoryDb, "wms").WaitFor(inventoryDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Inventory__Receiving__ReceivingLocationId", "b0000000-0000-0000-0000-000000000001")
    .WithEnvironment("Inventory__Receiving__QuarantineLocationId", "b0000000-0000-0000-0000-000000000003")
    .WithEnvironment("Inventory__Receiving__PutawayDestinationId", "b0000000-0000-0000-0000-000000000002")
    .WithEnvironment("Inventory__Receiving__PutawayAssignee", "c0000000-0000-0000-0000-000000000001");

var outbound = WithActiveUserChecker(WithJwtValidation(builder.AddProject<Projects.Wms_Outbound_Host_Local>("wms-outbound")))
    .WithReference(outboundDb, "wms").WaitFor(outboundDb)
    .WithReference(rabbitmq)
    .WithReference(masterData)
    .WithEnvironment("Services__MasterData__Grpc", masterData.GetEndpoint("https"))
    .WithEnvironment("Outbound__Picking__DefaultPickerId", "c0000000-0000-0000-0000-000000000001");

// Consumer hosts.
var reporting = WithActiveUserChecker(WithJwtValidation(builder.AddProject<Projects.Wms_Reporting_Host_Local>("wms-reporting")))
    .WithReference(reportingDb, "wms").WaitFor(reportingDb)
    .WithReference(rabbitmq);

var notifications = WithJwtValidation(builder.AddProject<Projects.Wms_Notifications_Host_Local>("wms-notifications"))
    .WithReference(notificationsDb, "wms").WaitFor(notificationsDb)
    .WithReference(rabbitmq)
    .WithReference(auth)
    .WithEnvironment("Services__Auth__Grpc", auth.GetEndpoint("https"));

// Gateway YARP sebagai edge lokal. Routing ke semua host memakai service discovery,
// lalu JWT divalidasi di gateway.
var gateway = WithJwtValidation(builder.AddProject<Projects.Wms_Gateway>("wms-gateway"))
    .WithReference(inbound)
    .WithReference(inventory)
    .WithReference(outbound)
    .WithReference(masterData)
    .WithReference(auth)
    .WithReference(reporting)
    .WithReference(notifications);

// WebUI Blazor lewat BFF gateway. Endpoint gateway diinjek dari AppHost.
builder.AddProject<Projects.Wms_WebUI>("wms-webui")
    .WithReference(gateway)
    .WithEnvironment("Bff__GatewayAddress", gateway.GetEndpoint("https"));

await builder.Build().RunAsync();

// Konfigurasi validasi JWT RS256 yang dipakai seragam oleh host REST dan gateway.
IResourceBuilder<ProjectResource> WithJwtValidation(IResourceBuilder<ProjectResource> project) =>
    project
        .WithEnvironment("Jwt__Issuer", JwtIssuer)
        .WithEnvironment("Jwt__Audience", JwtAudience)
        .WithEnvironment("Jwt__PublicKeyPem", jwtPublicPem);

// Endpoint AuthLookup untuk checker user aktif lintas host
IResourceBuilder<ProjectResource> WithActiveUserChecker(IResourceBuilder<ProjectResource> project) =>
    project
        .WithReference(auth)
        .WithEnvironment("Services__Auth__Grpc", auth.GetEndpoint("https"));
