using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Wms.Auth.Infrastructure.Security;
using Wms.Auth.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test validasi token Entra menggunakan JWKS statis tanpa akses database maupun jaringan.
public sealed class EntraTokenValidatorTests
{
    [Fact]
    public async Task Valid_id_token_yields_the_external_identity()
    {
        var token = TestEntraTokens.Mint("oid-123", preferredUsername: "op@contoso.com", name: "Operator");

        var result = await CreateValidator().ValidateAsync(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.ObjectId.Should().Be("oid-123");
        result.Value.UserPrincipalName.Should().Be("op@contoso.com");
        result.Value.DisplayName.Should().Be("Operator");
    }

    [Fact]
    public async Task A_token_for_a_different_audience_is_rejected()
    {
        var token = TestEntraTokens.Mint("oid-123", audience: "some-other-app");

        var result = await CreateValidator().ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task A_token_from_a_different_issuer_is_rejected()
    {
        var token = TestEntraTokens.Mint("oid-123", issuer: "https://evil.example/v2.0");

        var result = await CreateValidator().ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var token = TestEntraTokens.Mint(
            "oid-123",
            expires: TimeProvider.System.GetUtcNow().UtcDateTime.AddMinutes(-5));

        var result = await CreateValidator().ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task A_token_signed_by_a_foreign_key_is_rejected()
    {
        using var foreignKey = TestEntraTokens.CreateForeignKey();
        var token = TestEntraTokens.Mint("oid-123", signingKey: foreignKey);

        var result = await CreateValidator().ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task A_token_without_the_oid_claim_is_rejected()
    {
        var token = TestEntraTokens.Mint("ignored", includeObjectId: false);

        var result = await CreateValidator().ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_token_invalid");
    }

    [Fact]
    public async Task Validation_is_refused_when_federation_is_disabled()
    {
        var token = TestEntraTokens.Mint("oid-123");

        var result = await CreateValidator(enabled: false).ValidateAsync(token);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.entra_disabled");
    }

    private static EntraTokenValidator CreateValidator(bool enabled = true, string? audience = null)
    {
        var options = Options.Create(new EntraAuthOptions
        {
            Enabled = enabled,
            TenantId = "test-tenant",
            ClientId = audience ?? TestEntraTokens.Audience,
        });

        return new EntraTokenValidator(options, TestEntraTokens.ConfigurationManager());
    }
}
