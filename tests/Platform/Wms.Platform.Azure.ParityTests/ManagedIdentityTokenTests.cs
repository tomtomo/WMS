using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wms.Platform.Azure.Security;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan token service to service hanya berlaku untuk audience yang dituju dan bisa divalidasi tanpa memanggil layanan lain.
public sealed class ManagedIdentityTokenTests
{
    private const string Audience = "api://wms-masterdata";

    [Fact]
    public async Task Token_is_requested_for_the_audience_scope()
    {
        var credential = CredentialReturning("token-1", TimeSpan.FromHours(1));
        var provider = new ManagedIdentityTokenProvider(credential, TimeProvider.System);

        (await provider.GetTokenAsync(Audience)).Should().Be("token-1");

        await credential.Received(1).GetTokenAsync(
            Arg.Is<TokenRequestContext>(context => context.Scopes.Single() == "api://wms-masterdata/.default"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Audience_that_already_carries_a_default_scope_is_not_doubled()
    {
        var credential = CredentialReturning("token-1", TimeSpan.FromHours(1));
        var provider = new ManagedIdentityTokenProvider(credential, TimeProvider.System);

        await provider.GetTokenAsync("api://wms-auth/.default");

        await credential.Received(1).GetTokenAsync(
            Arg.Is<TokenRequestContext>(context => context.Scopes.Single() == "api://wms-auth/.default"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Valid_token_is_reused_instead_of_minted_again()
    {
        var credential = CredentialReturning("token-1", TimeSpan.FromHours(1));
        var provider = new ManagedIdentityTokenProvider(credential, TimeProvider.System);

        await provider.GetTokenAsync(Audience);
        await provider.GetTokenAsync(Audience);

        await credential.Received(1).GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_close_to_expiry_is_replaced_before_it_dies()
    {
        var timeProvider = new FakeTimeProvider();
        var credential = Substitute.For<TokenCredential>();
        credential.GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValueTask<AccessToken>(new AccessToken("token-1", timeProvider.GetUtcNow().AddMinutes(10))),
                new ValueTask<AccessToken>(new AccessToken("token-2", timeProvider.GetUtcNow().AddHours(1))));
        var provider = new ManagedIdentityTokenProvider(credential, timeProvider);

        (await provider.GetTokenAsync(Audience)).Should().Be("token-1");
        timeProvider.Advance(TimeSpan.FromMinutes(6));

        (await provider.GetTokenAsync(Audience)).Should().Be("token-2");
    }

    [Fact]
    public async Task Blank_audience_is_rejected_at_the_boundary()
    {
        var provider = new ManagedIdentityTokenProvider(Substitute.For<TokenCredential>(), TimeProvider.System);

        var token = () => provider.GetTokenAsync(" ");

        await token.Should().ThrowAsync<ArgumentException>();
    }

    private static TokenCredential CredentialReturning(string token, TimeSpan lifetime)
    {
        var credential = Substitute.For<TokenCredential>();
        credential.GetTokenAsync(Arg.Any<TokenRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AccessToken>(new AccessToken(token, DateTimeOffset.UtcNow.Add(lifetime))));
        return credential;
    }
}

// Pastikan token dari Entra id dapat divalidasi langsung dengan algoritma dan audience yang sesuai.
public sealed class ManagedIdentityTokenLiveTests
{
    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Minted_token_is_scoped_to_its_audience_and_carries_an_alg_pinned_header()
    {
        Skip.IfNot(
            Environment.GetEnvironmentVariable("WMS_PARITY_TOKEN_LIVE") is { Length: > 0 },
            "Token live tak diaktifkan (WMS_PARITY_TOKEN_LIVE).");

        // DefaultAzureCredential menggunakan alur autentikasi yang sama dengan Managed Identity di Azure.
        var provider = new ManagedIdentityTokenProvider(new DefaultAzureCredential(), TimeProvider.System);

        var vaultToken = await provider.GetTokenAsync("https://vault.azure.net");
        var storageToken = await provider.GetTokenAsync("https://storage.azure.com");

        var (vaultHeader, vaultPayload) = DecodeOffline(vaultToken);
        var (_, storagePayload) = DecodeOffline(storageToken);

        vaultHeader.GetProperty("alg").GetString().Should().Be("RS256", "callee memvalidasi dengan alg yang dipatok");

        // Token untuk resource yang berbeda harus memiliki audience berbeda, bukan satu token yang berlaku untuk semua layanan.
        var vaultAudience = vaultPayload.GetProperty("aud").GetString();
        var storageAudience = storagePayload.GetProperty("aud").GetString();
        vaultAudience.Should().NotBeNullOrWhiteSpace();
        storageAudience.Should().NotBeNullOrWhiteSpace().And.NotBe(vaultAudience);
    }

    [SkippableFact]
    [Trait("requires", "azure")]
    public async Task Repeated_request_for_the_same_audience_reuses_the_cached_token()
    {
        Skip.IfNot(
            Environment.GetEnvironmentVariable("WMS_PARITY_TOKEN_LIVE") is { Length: > 0 },
            "Token live tak diaktifkan (WMS_PARITY_TOKEN_LIVE).");
        var provider = new ManagedIdentityTokenProvider(new DefaultAzureCredential(), TimeProvider.System);

        var first = await provider.GetTokenAsync("https://vault.azure.net");
        var second = await provider.GetTokenAsync("https://vault.azure.net");

        second.Should().Be(first);
    }

    // Validasi offline: hanya base64url decode.
    private static (JsonElement Header, JsonElement Payload) DecodeOffline(string jwt)
    {
        var parts = jwt.Split('.');
        parts.Should().HaveCount(3, "token wajib berbentuk JWS compact");
        return (ParseSegment(parts[0]), ParseSegment(parts[1]));
    }

    private static JsonElement ParseSegment(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/').PadRight((segment.Length + 3) / 4 * 4, '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(padded))).RootElement.Clone();
    }
}
