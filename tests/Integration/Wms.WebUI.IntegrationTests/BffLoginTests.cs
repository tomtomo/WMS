using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wms.WebUI.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// RUN-05 + SEC-12: BFF login menyetel cookie sesi HttpOnly, dan JWT (token gateway) tak pernah bocor ke browser.
public sealed class BffLoginTests : IAsyncLifetime
{
    private const string StubAccessToken = "STUB-JWT-ACCESS-TOKEN-abc123";

    private WebApplication _gatewayStub = null!;
    private WebUiFactory _webUiFactory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var stubBuilder = WebApplication.CreateBuilder();
        stubBuilder.Logging.ClearProviders();
        _gatewayStub = stubBuilder.Build();
        _gatewayStub.Urls.Add("http://127.0.0.1:0");
        _gatewayStub.MapPost("/auth/v1/login", () => Results.Ok(
            new StubTokenResponse(StubAccessToken, DateTimeOffset.UtcNow.AddMinutes(15), "stub-refresh")));
        await _gatewayStub.StartAsync();

        var gatewayAddress = _gatewayStub.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();
        _webUiFactory = new WebUiFactory(gatewayAddress);
        _client = _webUiFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _webUiFactory.DisposeAsync();
        await _gatewayStub.DisposeAsync();
    }

    [Fact]
    public async Task Bff_login_sets_httponly_session_cookie_and_does_not_leak_jwt()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = "admin",
            ["password"] = "secret",
        });

        var response = await _client.PostAsync("/bff/login", form);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Redirect, "login body: {0}", body);
        response.Headers.Location!.OriginalString.Should().Be("/");

        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();
        setCookies.Should().Contain(cookie => cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        setCookies.Should().NotContain(
            cookie => cookie.Contains(StubAccessToken, StringComparison.Ordinal),
            "JWT disimpan server-side; cookie browser hanya pegang sesi terenkripsi");
        body.Should().NotContain(StubAccessToken, "token gateway tak boleh bocor ke response browser");
    }
}

// Respons stub gateway auth.v1 (camelCase → accessToken/expiresAt/refreshToken).
internal sealed record StubTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
