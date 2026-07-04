using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wms.MigrationRunner;

// MigrationWorker apply migration tiap DbContext modul lalu seed admin lalu hentikan host.
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<MigrationWorker>();

using var host = builder.Build();
await host.RunAsync();

return Environment.ExitCode;
