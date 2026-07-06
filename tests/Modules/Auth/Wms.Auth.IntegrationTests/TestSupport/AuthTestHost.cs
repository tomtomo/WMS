using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Local.Security;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Menyiapkan dependency Auth untuk test
internal static class AuthTestHost
{
    public static Dictionary<string, string?> ConfigValues(string connectionString) => new()
    {
        ["ConnectionStrings:wms"] = connectionString,
        ["Jwt:Issuer"] = TestJwtKeys.Issuer,
        ["Jwt:Audience"] = TestJwtKeys.Audience,
        ["Jwt:PublicKeyPem"] = TestJwtKeys.PublicKeyPem,
    };

    public static void AddAuthComposition(
        IServiceCollection services,
        IConfiguration configuration,
        TimeProvider? timeProvider = null,
        ISecretProvider? secretProvider = null)
    {
        services.AddLogging();
        services.AddApplicationBuildingBlocks(typeof(AuthPermissions).Assembly);
        services.AddBuildingBlocksInfrastructure("wms-auth-tests");
        services.AddAuthModule(configuration);

        services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddSingleton<ISecretProvider>(secretProvider ?? new TestSecretProvider());
        services.AddSingleton<TimeProvider>(timeProvider ?? TimeProvider.System);
    }

    public static ServiceProvider Build(string connectionString, TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(ConfigValues(connectionString)).Build();
        AddAuthComposition(services, configuration, timeProvider);
        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static async Task MigrateAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    }

    public static async Task SeedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await AuthSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<AuthDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
    }

    public static async Task MigrateAndSeedAsync(IServiceProvider provider)
    {
        await MigrateAsync(provider);
        await SeedAsync(provider);
    }
}
