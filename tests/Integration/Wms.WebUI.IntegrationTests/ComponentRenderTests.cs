using System.Net;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wms.WebUI.Components.Pages;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test render komponen dan tampilan tombol berdasarkan permission pengguna.
public sealed class ComponentRenderTests : TestContext
{
    public ComponentRenderTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
    }

    [Fact]
    public void Home_renders_title()
    {
        var cut = RenderComponent<Home>();

        cut.Markup.Should().Contain("TomSandbox WMS");
    }

    [Fact]
    public void Create_button_visible_when_user_has_CreateGR_permission()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("op");
        auth.SetClaims(new Claim("permission", "Inbound.CreateGR"));

        var cut = RenderComponent<GoodsReceipts>();

        HasButton(cut, "Buat").Should().BeTrue();
    }

    [Fact]
    public void Create_button_hidden_when_user_lacks_CreateGR_permission()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("op");

        var cut = RenderComponent<GoodsReceipts>();

        HasButton(cut, "Buat").Should().BeFalse();
    }

    private static bool HasButton(IRenderedComponent<GoodsReceipts> cut, string text) =>
        cut.FindAll("button").Any(button => button.TextContent.Contains(text, StringComparison.Ordinal));

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new StubHandler()) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"items":[],"total":0,"page":1,"pageSize":20}""", Encoding.UTF8, "application/json"),
            });
    }
}
