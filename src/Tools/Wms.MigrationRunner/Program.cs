using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.MigrationRunner;
using Wms.Platform.Local.Security;

// Jalankan migration dan seeding, lalu hentikan host.
var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// Daftarkan DbContext tiap modul
builder.Services.AddAllModules(builder.Configuration);

// Password hasher untuk seeding admin Auth.
builder.Services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

builder.Services.AddHostedService<MigrationWorker>();

using var host = builder.Build();
await host.RunAsync();

return Environment.ExitCode;
