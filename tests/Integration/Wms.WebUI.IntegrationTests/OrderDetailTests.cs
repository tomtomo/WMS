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

// Test OrderDetail melalui route OrderId dan typed client.
public sealed class OrderDetailTests : TestContext
{
    private static readonly Guid OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly string OrderJson =
        """{"orderId":"11111111-1111-1111-1111-111111111111","customerId":"22222222-2222-2222-2222-222222222222","status":"New","waveId":null,"lines":[{"sku":"SKU-MILK","qty":10,"allocatedQty":0,"allocationStatus":"Pending"}]}""";

    public OrderDetailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(OrderJson));
        Services.AddWmsApiClients();
        this.AddTestAuthorization().SetAuthorized("op");
    }

    [Fact]
    public void Renders_order_detail_with_lines()
    {
        var cut = RenderComponent<OrderDetail>(parameters => parameters.Add(page => page.OrderId, OrderId));
        cut.Markup.Should().Contain("Order Detail");
        cut.Markup.Should().Contain("SKU-MILK");
    }

    private sealed class StubHttpClientFactory(string json) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(json)) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }
}
