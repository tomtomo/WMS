using System.Net;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wms.WebUI.Components.Pages.Outbound;
using Wms.WebUI.Services;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test WaveDetail melalui route WaveId dan typed client; halaman ini tidak membutuhkan popover.
public sealed class WaveDetailTests : TestContext
{
    private static readonly Guid WaveId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly string WaveJson =
        """{"waveId":"33333333-3333-3333-3333-333333333333","warehouseId":"a0000000-0000-0000-0000-000000000001","status":"Ready","cancelReason":null,"orderIds":["11111111-1111-1111-1111-111111111111"],"pickingTaskCount":1,"completedPickingTaskCount":0}""";

    private static readonly string EmptyPage = """{"items":[],"totalCount":0,"page":1,"pageSize":10}""";

    public WaveDetailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
        Services.AddWmsApiClients();
        this.AddTestAuthorization().SetAuthorized("op");
    }

    [Fact]
    public void Renders_wave_detail_with_status()
    {
        var cut = RenderComponent<WaveDetail>(parameters => parameters.Add(page => page.WaveId, WaveId));
        cut.Markup.Should().Contain("Wave Detail");
        cut.Markup.Should().Contain("Ready");
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler()) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = path.StartsWith("/outbound/v1/waves/", StringComparison.Ordinal) ? WaveJson : EmptyPage;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
