using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wms.Platform.Local.Secrets;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

public sealed class EnvSecretProviderTests
{
    [Fact]
    public async Task Secret_present_in_configuration_is_returned()
    {
        var provider = CreateProvider(new KeyValuePair<string, string?>("Secrets:api-key", "s3cret-dev"));

        var secret = await provider.GetSecretAsync("api-key");

        secret.Should().Be("s3cret-dev");
    }

    [Fact]
    public async Task Missing_secret_fails_secure_instead_of_leaking_null()
    {
        var provider = CreateProvider();

        var act = () => provider.GetSecretAsync("payment-gateway-key");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Secrets__payment-gateway-key*");
    }

    [Fact]
    public async Task Missing_signing_key_falls_back_to_generated_rsa_keypair()
    {
        var provider = CreateProvider();

        var pem = await provider.GetSecretAsync("jwt-signing-key");

        pem.Should().StartWith("-----BEGIN PRIVATE KEY-----");
    }

    [Fact]
    public async Task Generated_dev_signing_key_is_stable_within_process()
    {
        var provider = CreateProvider();

        var first = await provider.GetSecretAsync("jwt-signing-key");
        var second = await provider.GetSecretAsync("jwt-signing-key");

        second.Should().Be(first);
    }

    [Fact]
    public async Task Configured_signing_key_wins_over_generated_fallback()
    {
        var provider = CreateProvider(new KeyValuePair<string, string?>("Secrets:jwt-signing-key", "pem-from-env"));

        var secret = await provider.GetSecretAsync("jwt-signing-key");

        secret.Should().Be("pem-from-env");
    }

    private static EnvSecretProvider CreateProvider(params KeyValuePair<string, string?>[] values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new EnvSecretProvider(
            configuration,
            Options.Create(new EnvSecretOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EnvSecretProvider>.Instance);
    }
}
