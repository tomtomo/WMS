using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Wms.Auth.Application;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Shared.Security;

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

    // Aktifkan login Entra dengan JWKS khusus test agar alurnya bisa diuji tanpa akses jaringan.
    public static ServiceProvider BuildWithEntra(
        string connectionString,
        IConfigurationManager<OpenIdConnectConfiguration> entraConfigurationManager,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        var values = ConfigValues(connectionString);
        values["Entra:Enabled"] = "true";
        values["Entra:TenantId"] = "test-tenant";
        values["Entra:ClientId"] = TestEntraTokens.Audience;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        AddAuthComposition(services, configuration, timeProvider);
        services.AddSingleton<ICurrentUser>(new FixedCurrentUser());
        services.RemoveAll<IConfigurationManager<OpenIdConnectConfiguration>>();
        services.AddSingleton(entraConfigurationManager);

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
