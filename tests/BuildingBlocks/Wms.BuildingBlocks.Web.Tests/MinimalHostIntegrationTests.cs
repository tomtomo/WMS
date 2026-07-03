using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

// Host minimal dengan composition penuh (AddWebBuildingBlocks + AddJwtBearerRs256 + UseWebBuildingBlocks):
public sealed class MinimalHostIntegrationTests : IClassFixture<MinimalHostFixture>
{
    private readonly MinimalHostFixture _fixture;

    public MinimalHostIntegrationTests(MinimalHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Result_failure_maps_to_problem_details_with_error_code_and_correlation()
    {
        using var client = _fixture.App.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fail");
        request.Headers.Add(CorrelationId.HeaderName, "corr-xyz");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle().Which.Should().Be("corr-xyz");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("inventory.not_found");
        doc.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-xyz");
    }

    [Fact]
    public async Task Anonymous_request_resolves_current_user_to_SYSTEM()
    {
        using var client = _fixture.App.GetTestClient();

        var body = await client.GetStringAsync(new Uri("/whoami", UriKind.Relative));

        body.Should().Be(ICurrentUser.SystemActor);
    }

    [Fact]
    public async Task Valid_rs256_token_is_accepted_and_populates_current_user()
    {
        using var client = _fixture.App.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.Rs256Token);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("user-77");
    }

    [Fact]
    public async Task Hs256_token_is_rejected_by_alg_pinning_as_problem_details()
    {
        using var client = _fixture.App.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fixture.Hs256Token);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}

// Fixture: RSA keypair -> public key ke config host (validate), private key mint token RS256; plus token HS256 (ditolak).
public sealed class MinimalHostFixture : IAsyncLifetime
{
    private const string Issuer = "wms-auth";
    private const string Audience = "wms-api";

    public WebApplication App { get; private set; } = null!;

    public string Rs256Token { get; private set; } = string.Empty;

    public string Hs256Token { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        using var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        Rs256Token = Mint("user-77", new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256));
        Hs256Token = Mint(
            "evil",
            new SigningCredentials(
                new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)),
                SecurityAlgorithms.HmacSha256));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Services.AddWebBuildingBlocks();
        builder.Services.AddJwtBearerRs256(BuildConfiguration(publicKeyPem));
        builder.Services.AddAuthorization();
        var app = builder.Build();

        app.UseWebBuildingBlocks();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/fail", (HttpContext ctx) =>
            Result.NotFound(new Error("inventory.not_found", "Stok tak ada.")).ToProblem(ctx));
        app.MapGet("/whoami", (ICurrentUser user) => user.UserId);
        app.MapGet("/secure", (ICurrentUser user) => user.UserId).RequireAuthorization();

        await app.StartAsync();
        App = app;
    }

    public async Task DisposeAsync() => await App.DisposeAsync();

    private static IConfiguration BuildConfiguration(string publicKeyPem) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:PublicKeyPem"] = publicKeyPem,
            })
            .Build();

    private static string Mint(string subject, SigningCredentials credentials)
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("sub", subject)],
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
