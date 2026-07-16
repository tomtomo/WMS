using System.Net;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Wms.WebUI.Components.Pages.Reporting;
using Wms.WebUI.Services;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test render halaman Reporting read-only dengan data PagedResult kosong.
public sealed class ReportingPagesTests : TestContext
{
    private static readonly string EmptyPage = """{"items":[],"totalCount":0,"page":1,"pageSize":10}""";

    public ReportingPagesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(EmptyPage));
        Services.AddWmsApiClients();
        this.AddTestAuthorization().SetAuthorized("op");
    }

    [Fact]
    public void StockOnHand_renders_title()
    {
        RenderPage<StockOnHand>().Markup.Should().Contain("Stock on Hand");
    }

    [Fact]
    public void DispatchSummary_renders_title()
    {
        RenderPage<DispatchSummary>().Markup.Should().Contain("Dispatch Summary");
    }

    [Fact]
    public void OperatorProductivity_renders_title()
    {
        RenderPage<OperatorProductivity>().Markup.Should().Contain("Operator Productivity");
    }

    [Fact]
    public void SupplierPerformance_renders_title()
    {
        RenderPage<SupplierPerformance>().Markup.Should().Contain("Supplier Performance");
    }

    // MudTablePager membutuhkan MudPopoverProvider dari MainLayout.
    private IRenderedFragment RenderPage<T>()
        where T : IComponent =>
        Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<T>(1);
            builder.CloseComponent();
        });

    private sealed class StubHttpClientFactory(string pageJson) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(pageJson)) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler(string pageJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(pageJson, Encoding.UTF8, "application/json"),
            });
    }
}
