// Aspire AppHost — composition root inner-loop lokal: satu DistributedApplication.Run() mengaktifkan
// Postgres, RabbitMQ, dashboard OTLP, MigrationRunner.
var builder = DistributedApplication.CreateBuilder(args);

// Postgres: satu DB fisik "wms"
var wmsDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("wms");

// RabbitMQ.
builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// MigrationRunner: apply migration per-module sekali saat boot sebelum host.
builder.AddProject<Projects.Wms_MigrationRunner>("migrations")
    .WithReference(wmsDb)
    .WaitFor(wmsDb);

// Host modul.
await builder.Build().RunAsync();
