using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Wms.Platform.Azure.Secrets;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan startup langsung gagal jika secret tidak tersedia atau kosong.
// Verifikasi JWT menggunakan public key tanpa perlu mengakses Key Vault setiap kali request.
public sealed class SecretProviderTests
{
    private const string SigningKeySecretName = "jwt-signing-key";

    [Fact]
    public async Task Existing_secret_is_returned_as_is()
    {
        var provider = new KeyVaultSecretProvider(ClientReturning("nilai-rahasia"));

        (await provider.GetSecretAsync(SigningKeySecretName)).Should().Be("nilai-rahasia");
    }

    [Fact]
    public async Task Missing_secret_fails_secure_instead_of_returning_null()
    {
        var client = Substitute.For<SecretClient>();
        client.GetSecretAsync(SigningKeySecretName, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestFailedException(404, "SecretNotFound"));
        var provider = new KeyVaultSecretProvider(client);

        var read = () => provider.GetSecretAsync(SigningKeySecretName);

        await read.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Empty_secret_fails_secure_instead_of_being_used_as_a_key()
    {
        var provider = new KeyVaultSecretProvider(ClientReturning(" "));

        var read = () => provider.GetSecretAsync(SigningKeySecretName);

        await read.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Blank_secret_name_is_rejected_at_the_boundary()
    {
        var provider = new KeyVaultSecretProvider(Substitute.For<SecretClient>());

        var read = () => provider.GetSecretAsync(" ");

        await read.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Signing_touches_the_vault_but_verifying_stays_offline()
    {
        using var rsa = RSA.Create(2048);
        var client = ClientReturning(rsa.ExportPkcs8PrivateKeyPem());
        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        ISecretProvider provider = new KeyVaultSecretProvider(client);

        var signature = await SignAsync(provider, "gr-001");
        var verified = VerifyOffline(publicKeyPem, "gr-001", signature);

        verified.Should().BeTrue();

        // Public key dipakai untuk verifikasi, jadi Key Vault hanya diakses saat membuat signature.
        await client.Received(1).GetSecretAsync(SigningKeySecretName, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static SecretClient ClientReturning(string secretValue)
    {
        var client = Substitute.For<SecretClient>();
        client.GetSecretAsync(SigningKeySecretName, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Response.FromValue(new KeyVaultSecret(SigningKeySecretName, secretValue), Substitute.For<Response>()));
        return client;
    }

    private static async Task<byte[]> SignAsync(ISecretProvider provider, string payload)
    {
        var privateKeyPem = await provider.GetSecretAsync(SigningKeySecretName);
        using var signer = RSA.Create();
        signer.ImportFromPem(privateKeyPem);
        return signer.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyOffline(string publicKeyPem, string payload, byte[] signature)
    {
        using var verifier = RSA.Create();
        verifier.ImportFromPem(publicKeyPem);
        return verifier.VerifyData(
            Encoding.UTF8.GetBytes(payload),
            signature,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }
}

// Key Vault RBAC tidak memiliki emulator, jadi test dijalankan memakai resource nyata.
// Test ini memastikan secret dapat dibaca dan akses tetap ditolak saat konfigurasi tidak valid.
public sealed class KeyVaultLiveTests
{
    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Secret_stored_in_the_vault_is_read_back_through_managed_identity()
    {
        Skip.IfNot(AzureLiveSettings.HasKeyVault, "Key Vault live tak dikonfigurasi (WMS_PARITY_KEYVAULT_URI).");
        var provider = new KeyVaultSecretProvider(NewClient());

        var secret = await provider.GetSecretAsync("wms-parity-probe");

        secret.Should().Be("nilai-parity");
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Secret_that_does_not_exist_fails_secure()
    {
        Skip.IfNot(AzureLiveSettings.HasKeyVault, "Key Vault live tak dikonfigurasi (WMS_PARITY_KEYVAULT_URI).");
        var provider = new KeyVaultSecretProvider(NewClient());

        var read = () => provider.GetSecretAsync($"tidak-ada-{Guid.NewGuid():N}");

        await read.Should().ThrowAsync<InvalidOperationException>();
    }

    private static SecretClient NewClient() =>
        new(new Uri(AzureLiveSettings.KeyVaultUri!), new DefaultAzureCredential());
}
