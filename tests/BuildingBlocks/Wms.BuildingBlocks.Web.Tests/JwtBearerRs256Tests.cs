using System.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wms.BuildingBlocks.Web.Auth;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class JwtBearerRs256Tests
{
    [Fact]
    public void BuildValidationParameters_pins_rs256_and_enables_all_validations()
    {
        using var rsa = RSA.Create(2048);
        var options = new JwtBearerRs256Options
        {
            Issuer = "wms-auth",
            Audience = "wms-api",
            PublicKeyPem = rsa.ExportSubjectPublicKeyInfoPem(),
        };

        var parameters = JwtBearerSetup.BuildValidationParameters(options);

        parameters.ValidAlgorithms.Should().ContainSingle().Which.Should().Be(SecurityAlgorithms.RsaSha256);
        parameters.IssuerSigningKey.Should().BeOfType<RsaSecurityKey>();
        parameters.RequireSignedTokens.Should().BeTrue();
        parameters.ValidateIssuer.Should().BeTrue();
        parameters.ValidateAudience.Should().BeTrue();
        parameters.ValidateLifetime.Should().BeTrue();
        parameters.ValidIssuer.Should().Be("wms-auth");
        parameters.ValidAudience.Should().Be("wms-api");
    }

    [Fact]
    public void BuildValidationParameters_fails_secure_when_public_key_missing()
    {
        var options = new JwtBearerRs256Options { Issuer = "i", Audience = "a", PublicKeyPem = string.Empty };

        var act = () => JwtBearerSetup.BuildValidationParameters(options);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddJwtBearerRs256_options_validation_fails_fast_when_public_key_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "wms-auth",
                ["Jwt:Audience"] = "wms-api",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddJwtBearerRs256(configuration);
        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IOptions<JwtBearerRs256Options>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }
}
