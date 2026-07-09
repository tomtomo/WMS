using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Wms.Gateway.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Gateway.IntegrationTests;

// Memastikan gateway menolak request anonim, meneruskan request bertoken, membawa Authorization header, dan menjaga correlation id.
public sealed class GatewayRoutingTests : IAsyncLifetime
{
    private static readonly RSA _rsa = RSA.Create(2048);

    private WebApplication _downstream = null!;
    private GatewayFactory _gatewayFactory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var downstreamBuilder = WebApplication.CreateBuilder();
        downstreamBuilder.Logging.ClearProviders();
        _downstream = downstreamBuilder.Build();
        _downstream.Urls.Add("http://127.0.0.1:0");
        _downstream.MapGet("/ping", (HttpContext context) => Results.Ok(new EchoResponse(
            context.Request.Headers["X-Correlation-ID"].ToString(),
            context.Request.Headers.Authorization.ToString())));
        await _downstream.StartAsync();

        var downstreamAddress = _downstream.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _gatewayFactory = new GatewayFactory(_rsa.ExportSubjectPublicKeyInfoPem(), downstreamAddress);
        _client = _gatewayFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _gatewayFactory.DisposeAsync();
        await _downstream.DisposeAsync();
    }

    [Fact]
    public async Task Anonymous_request_to_protected_route_is_rejected_401()
    {
        var response = await _client.GetAsync("/svc/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Valid_jwt_is_proxied_and_bearer_forwarded_downstream()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());

        var response = await _client.GetAsync("/svc/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var echo = await response.Content.ReadFromJsonAsync<EchoResponse>();
        echo!.Authorization.Should().StartWith("Bearer ", "auth-forward: bearer diteruskan ke downstream");
    }

    [Fact]
    public async Task Correlation_id_is_injected_when_absent_and_propagated_downstream()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());

        var response = await _client.GetAsync("/svc/ping");

        var echo = await response.Content.ReadFromJsonAsync<EchoResponse>();
        echo!.CorrelationId.Should().NotBeNullOrEmpty("gateway inject X-Correlation-ID bila absen → forward downstream");
        response.Headers.GetValues("X-Correlation-ID").Should().ContainSingle();
    }

    [Fact]
    public async Task Existing_correlation_id_is_propagated_downstream()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());
        _client.DefaultRequestHeaders.Add("X-Correlation-ID", "corr-abc-123");

        var response = await _client.GetAsync("/svc/ping");

        var echo = await response.Content.ReadFromJsonAsync<EchoResponse>();
        echo!.CorrelationId.Should().Be("corr-abc-123", "gateway propagate correlation-id yang sudah ada");
    }

    private static string MintToken()
    {
        var credentials = new SigningCredentials(new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: "wms-local",
            audience: "wms-local",
            claims: [new Claim("sub", "gateway-test-user")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Respons kecil dari downstream test untuk memastikan Authorization dan correlation id ikut diteruskan.
internal sealed record EchoResponse(string CorrelationId, string Authorization);
