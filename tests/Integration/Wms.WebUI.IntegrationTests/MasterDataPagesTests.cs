using System.Net;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Wms.WebUI.Components.Pages.MasterData;
using Wms.WebUI.Services;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test render halaman Master Data dan visibilitas tombol Tambah berdasarkan permission Manage*.
public sealed class MasterDataPagesTests : TestContext
{
    private const string ManageProduct = "MasterData.ManageProduct";
    private const string ManageLocation = "MasterData.ManageLocation";
    private const string ManageWarehouse = "MasterData.ManageWarehouse";

    private static readonly string EmptyPage = """{"items":[],"totalCount":0,"page":1,"pageSize":10}""";

    public MasterDataPagesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(EmptyPage));
        Services.AddWmsApiClients();
    }

    [Fact]
    public void Products_page_renders_title()
    {
        Authorize();
        RenderPage<Products>().Markup.Should().Contain("Products");
    }

    [Fact]
    public void Products_add_button_shown_only_with_permission()
    {
        Authorize(ManageProduct);
        HasButton(RenderPage<Products>(), "Tambah Product").Should().BeTrue();
    }

    [Fact]
    public void Products_add_button_hidden_without_permission()
    {
        Authorize();
        HasButton(RenderPage<Products>(), "Tambah Product").Should().BeFalse();
    }

    [Fact]
    public void Warehouses_page_renders_title()
    {
        Authorize();
        RenderPage<Warehouses>().Markup.Should().Contain("Warehouses");
    }

    [Fact]
    public void Warehouses_add_button_shown_only_with_permission()
    {
        Authorize(ManageWarehouse);
        HasButton(RenderPage<Warehouses>(), "Tambah Warehouse").Should().BeTrue();
    }

    [Fact]
    public void Warehouses_add_button_hidden_without_permission()
    {
        Authorize();
        HasButton(RenderPage<Warehouses>(), "Tambah Warehouse").Should().BeFalse();
    }

    [Fact]
    public void Locations_page_renders_title()
    {
        Authorize();
        RenderPage<Locations>().Markup.Should().Contain("Locations");
    }

    [Fact]
    public void Locations_add_button_shown_only_with_permission()
    {
        Authorize(ManageLocation);
        HasButton(RenderPage<Locations>(), "Tambah Location").Should().BeTrue();
    }

    [Fact]
    public void Locations_add_button_hidden_without_permission()
    {
        Authorize();
        HasButton(RenderPage<Locations>(), "Tambah Location").Should().BeFalse();
    }

    private static bool HasButton(IRenderedFragment cut, string text) =>
        cut.FindAll("button").Any(button => button.TextContent.Contains(text, StringComparison.Ordinal));

    // MudTablePager membutuhkan MudPopoverProvider; di aplikasi provider ini disediakan MainLayout.
    private IRenderedFragment RenderPage<T>()
        where T : IComponent =>
        Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<T>(1);
            builder.CloseComponent();
        });

    private void Authorize(params string[] permissions)
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("op");
        auth.SetClaims([.. permissions.Select(permission => new Claim("permission", permission))]);
    }

    private sealed class StubHttpClientFactory(string pageJson) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(pageJson)) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler(string pageJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var status = request.Method == HttpMethod.Get ? HttpStatusCode.OK : HttpStatusCode.Created;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(pageJson, Encoding.UTF8, "application/json"),
            });
        }
    }
}
