using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure.Security;
using Wms.Auth.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Web.Auth;
using Xunit;

namespace Wms.Auth.IntegrationTests;

// Test JWT dengan RS256.
public sealed class JwtIssuanceTests
{
    private static readonly JwtIssuerOptions _options = new()
    {
        Issuer = TestJwtKeys.Issuer,
        Audience = TestJwtKeys.Audience,
        SigningKeySecretName = TestJwtKeys.SigningKeySecretName,
        AccessTokenLifetimeMinutes = 15,
    };

    [Fact]
    public async Task An_issued_token_is_rs256_and_validates_offline_with_the_public_key()
    {
        var accessToken = await Issuer().IssueAsync(AUser(), ["Auth.ManageUser"], CancellationToken.None);

        var handler = new JsonWebTokenHandler();
        var validation = await handler.ValidateTokenAsync(accessToken.Token, ValidationParameters());

        validation.IsValid.Should().BeTrue();
        handler.ReadJsonWebToken(accessToken.Token).Alg.Should().Be(SecurityAlgorithms.RsaSha256);
    }

    [Fact]
    public async Task The_token_embeds_subject_username_and_effective_permissions()
    {
        var user = AUser();

        var accessToken = await Issuer().IssueAsync(user, ["Auth.ManageUser", "Inbound.PostGR"], CancellationToken.None);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken.Token);
        jwt.GetClaim("sub").Value.Should().Be(user.Id.Value.ToString());
        jwt.GetClaim("username").Value.Should().Be("admin");
        jwt.Claims.Where(claim => claim.Type == "permission").Select(claim => claim.Value)
            .Should().HaveCount(2).And.Contain("Auth.ManageUser").And.Contain("Inbound.PostGR");
    }

    [Fact]
    public async Task The_validator_rejects_an_alg_none_token()
    {
        var unsigned = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            TestJwtKeys.Issuer, TestJwtKeys.Audience, [new Claim("sub", "attacker")], expires: DateTime.UtcNow.AddMinutes(5)));

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(unsigned, ValidationParameters());

        validation.IsValid.Should().BeFalse("token alg=none harus ditolak (RequireSignedTokens)");
    }

    [Fact]
    public async Task The_validator_rejects_an_hs256_alg_substitution_token()
    {
        var symmetricKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
        var hs256 = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            TestJwtKeys.Issuer,
            TestJwtKeys.Audience,
            [new Claim("sub", "attacker")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256)));

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(hs256, ValidationParameters());

        validation.IsValid.Should().BeFalse("alg-substitution HS256 harus ditolak (ValidAlgorithms=[RS256])");
    }

    [Fact]
    public async Task A_null_signing_key_is_fail_secure_and_never_issues_an_unsigned_token()
    {
        var act = async () => await Issuer(new NullSecretProvider())
            .IssueAsync(AUser(), [], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static TokenValidationParameters ValidationParameters() =>
        JwtBearerSetup.BuildValidationParameters(new JwtBearerRs256Options
        {
            Issuer = TestJwtKeys.Issuer,
            Audience = TestJwtKeys.Audience,
            PublicKeyPem = TestJwtKeys.PublicKeyPem,
        });

    private static JwtTokenIssuer Issuer(ISecretProvider? secretProvider = null) =>
        new(secretProvider ?? new TestSecretProvider(), Options.Create(_options), TimeProvider.System);

    private static User AUser() =>
        User.Create(UserId.Create(Guid.NewGuid()).Value, "admin", "admin@wms.local", "hash", [], [Guid.NewGuid()]).Value;
}
