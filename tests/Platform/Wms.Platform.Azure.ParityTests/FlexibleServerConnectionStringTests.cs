using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.Persistence;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Command side hanya mengganti connection string tanpa mengubah skema, migration, atau konfigurasi lainnya.
// Adapter Azure menambahkan TLS wajib dan mengambil password dari Key Vault.
public sealed class FlexibleServerConnectionStringTests
{
    private const string ConfiguredConnectionString =
        "Host=wms.postgres.database.azure.com;Database=wms_inbound;Username=wms_app";

    [Fact]
    public async Task Connection_string_requires_tls_and_a_verified_certificate_by_default()
    {
        var factory = NewFactory(new FlexibleServerOptions(), SecretProviderReturning("s3cret"));

        var built = new NpgsqlConnectionStringBuilder(await factory.CreateAsync());

        // SslMode.Require hanya mewajibkan enkripsi, sedangkan hanya VerifyFull yang memeriksa sertifikat dan hostname.
        built.SslMode.Should().Be(SslMode.VerifyFull);
    }

    [Fact]
    public async Task Password_is_pulled_from_the_secret_store_not_from_configuration()
    {
        var secretProvider = SecretProviderReturning("s3cret");
        var options = new FlexibleServerOptions { PasswordSecretName = "flexible-server-password" };

        var built = new NpgsqlConnectionStringBuilder(await NewFactory(options, secretProvider).CreateAsync());

        built.Password.Should().Be("s3cret");
        ConfiguredConnectionString.Should().NotContain("Password", "kredensial tak pernah disimpan di konfigurasi");
        await secretProvider.Received(1).GetSecretAsync("flexible-server-password", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Secret_store_is_left_alone_when_no_password_secret_is_configured()
    {
        var secretProvider = SecretProviderReturning("s3cret");

        await NewFactory(new FlexibleServerOptions(), secretProvider).CreateAsync();

        await secretProvider.DidNotReceive().GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_password_secret_fails_secure_instead_of_connecting_without_one()
    {
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Secret tidak ditemukan di Key Vault."));
        var options = new FlexibleServerOptions { PasswordSecretName = "flexible-server-password" };

        var create = () => NewFactory(options, secretProvider).CreateAsync();

        await create.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Null_password_secret_fails_secure_instead_of_connecting_without_one()
    {
        // ISecretProvider dapat mengembalikan null, tetapi adapter harus menganggapnya sebagai password yang tidak tersedia.
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var options = new FlexibleServerOptions { PasswordSecretName = "flexible-server-password" };

        var create = () => NewFactory(options, secretProvider).CreateAsync();

        await create.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Missing_connection_string_fails_fast_at_startup()
    {
        var factory = new FlexibleServerConnectionStringFactory(
            new ConfigurationBuilder().Build(),
            Options.Create(new FlexibleServerOptions()),
            SecretProviderReturning("s3cret"));

        var create = () => factory.CreateAsync();

        await create.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Module_topology_stays_one_connection_string_per_module()
    {
        // Topologi satu database per modul diteruskan apa adanya ke cloud, bukan disatukan menjadi satu database fisik.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:inbounddb"] = ConfiguredConnectionString,
                ["ConnectionStrings:inventorydb"] = "Host=wms.postgres.database.azure.com;Database=wms_inventory;Username=wms_app",
            })
            .Build();
        var factory = new FlexibleServerConnectionStringFactory(
            configuration,
            Options.Create(new FlexibleServerOptions { ConnectionStringName = "inventorydb" }),
            SecretProviderReturning("s3cret"));

        var built = new NpgsqlConnectionStringBuilder(await factory.CreateAsync());

        built.Database.Should().Be("wms_inventory");
    }

    private static ISecretProvider SecretProviderReturning(string secret)
    {
        var secretProvider = Substitute.For<ISecretProvider>();
        secretProvider.GetSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(secret);
        return secretProvider;
    }

    private static FlexibleServerConnectionStringFactory NewFactory(
        FlexibleServerOptions options,
        ISecretProvider secretProvider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{options.ConnectionStringName}"] = ConfiguredConnectionString,
            })
            .Build();
        return new FlexibleServerConnectionStringFactory(configuration, Options.Create(options), secretProvider);
    }
}
