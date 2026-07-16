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
using Wms.WebUI.Components.Pages.Outbound;
using Wms.WebUI.Services;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test render, otorisasi tombol, dan alur POST utama pada halaman Outbound.
public sealed class OutboundPagesTests : TestContext
{
    private const string CreateOrder = "Outbound.CreateOrder";
    private const string CreateWave = "Outbound.CreateWave";
    private const string DispatchWave = "Outbound.DispatchWave";
    private const string CompletePicking = "Outbound.CompletePickingTask";

    private static readonly string OneBacklogOrder =
        """[{"orderId":"11111111-1111-1111-1111-111111111111","customerId":"22222222-2222-2222-2222-222222222222","status":"New","waveId":null,"lines":[{"sku":"SKU-MILK","qty":10,"allocatedQty":0,"allocationStatus":"Pending"}]}]""";

    private static readonly string OneActiveWave =
        """[{"waveId":"33333333-3333-3333-3333-333333333333","warehouseId":"a0000000-0000-0000-0000-000000000001","status":"Active","orderCount":1}]""";

    private static readonly string OneReadyWave =
        """[{"waveId":"33333333-3333-3333-3333-333333333333","warehouseId":"a0000000-0000-0000-0000-000000000001","status":"Ready","orderCount":1}]""";

    private static readonly string OnePickingTask =
        """[{"pickingTaskId":"44444444-4444-4444-4444-444444444444","waveId":"33333333-3333-3333-3333-333333333333","stockId":"55555555-5555-5555-5555-555555555555","sourceLocationId":"b0000000-0000-0000-0000-000000000002","sku":"SKU-MILK","batch":null,"qty":10,"assignedTo":"c0000000-0000-0000-0000-000000000001","status":"Assigned","actualQty":null,"stagingLocationId":null}]""";

    private readonly CapturingHandler _handler = new();

    public OutboundPagesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new CapturingHttpClientFactory(_handler));
        Services.AddWmsApiClients();
    }

    [Fact]
    public void Outbound_orders_page_renders_title()
    {
        Authorize();
        RenderPage<OutboundOrders>().Markup.Should().Contain("Outbound Orders");
    }

    [Fact]
    public void Create_order_button_shown_only_with_permission()
    {
        Authorize(CreateOrder);
        HasButton(RenderPage<OutboundOrders>(), "Buat Order").Should().BeTrue();
    }

    [Fact]
    public void Create_order_button_hidden_without_permission()
    {
        Authorize();
        HasButton(RenderPage<OutboundOrders>(), "Buat Order").Should().BeFalse();
    }

    [Fact]
    public void Create_order_click_posts_to_outbound_orders()
    {
        Authorize(CreateOrder);
        var cut = RenderPage<OutboundOrders>();

        ClickButton(cut, "Buat Order");

        cut.WaitForAssertion(() => _handler.Posted("/outbound/v1/outbound-orders").Should().BeTrue());
    }

    [Fact]
    public void Waves_page_renders_title()
    {
        Authorize();
        RenderPage<Waves>().Markup.Should().Contain("Waves");
    }

    [Fact]
    public void Create_wave_button_shown_only_with_permission()
    {
        _handler.BacklogJson = OneBacklogOrder;
        Authorize(CreateWave);
        HasButton(RenderPage<Waves>(), "Buat Wave").Should().BeTrue();
    }

    [Fact]
    public void Create_wave_button_hidden_without_permission()
    {
        _handler.BacklogJson = OneBacklogOrder;
        Authorize();
        HasButton(RenderPage<Waves>(), "Buat Wave").Should().BeFalse();
    }

    [Fact]
    public void Create_wave_click_posts_to_waves()
    {
        _handler.BacklogJson = OneBacklogOrder;
        Authorize(CreateWave);
        var cut = RenderPage<Waves>();

        ClickButton(cut, "Buat Wave");

        cut.WaitForAssertion(() => _handler.Posted("/outbound/v1/waves").Should().BeTrue());
    }

    [Fact]
    public void Dispatch_click_posts_to_wave_dispatch()
    {
        // Tombol dispatch hanya muncul setelah wave berstatus Ready.
        _handler.WavesJson = OneReadyWave;
        Authorize(DispatchWave);
        var cut = RenderPage<Waves>();

        ClickButton(cut, "Dispatch");

        cut.WaitForAssertion(() =>
            _handler.PostedMatching(path => path.StartsWith("/outbound/v1/waves/", StringComparison.Ordinal)
                && path.EndsWith("/dispatch", StringComparison.Ordinal)).Should().BeTrue());
    }

    [Fact]
    public void Dispatch_hidden_and_hint_shown_when_wave_active()
    {
        // Wave yang masih Active belum bisa didispatch, jadi tampilkan hint tanpa tombol aksi.
        _handler.WavesJson = OneActiveWave;
        Authorize(DispatchWave);
        var cut = RenderPage<Waves>();

        HasButton(cut, "Dispatch").Should().BeFalse();
        cut.Markup.Should().Contain("Menunggu picking");
    }

    [Fact]
    public void Picking_page_renders_title()
    {
        Authorize();
        RenderPage<Picking>().Markup.Should().Contain("Picking");
    }

    [Fact]
    public void Complete_button_shown_only_with_permission()
    {
        _handler.PickingJson = OnePickingTask;
        Authorize(CompletePicking);
        HasButton(RenderPage<Picking>(), "Complete").Should().BeTrue();
    }

    [Fact]
    public void Complete_button_hidden_without_permission()
    {
        _handler.PickingJson = OnePickingTask;
        Authorize();
        HasButton(RenderPage<Picking>(), "Complete").Should().BeFalse();
    }

    [Fact]
    public void Complete_click_posts_to_picking_task_complete()
    {
        _handler.PickingJson = OnePickingTask;
        Authorize(CompletePicking);
        var cut = RenderPage<Picking>();

        ClickButton(cut, "Complete");

        cut.WaitForAssertion(() =>
            _handler.PostedMatching(path => path.StartsWith("/outbound/v1/picking-tasks/", StringComparison.Ordinal)
                && path.EndsWith("/complete", StringComparison.Ordinal)).Should().BeTrue());
    }

    private static bool HasButton(IRenderedFragment cut, string text) =>
        cut.FindAll("button").Any(button => button.TextContent.Contains(text, StringComparison.Ordinal));

    private static void ClickButton(IRenderedFragment cut, string text) =>
        cut.FindAll("button").First(button => button.TextContent.Contains(text, StringComparison.Ordinal)).Click();

    // MudSelect dan MudTablePager membutuhkan MudPopoverProvider dari MainLayout.
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

    private sealed class CapturingHttpClientFactory(CapturingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private const string ProductsPage =
            """{"items":[{"sku":"SKU-MILK","name":"Fresh Milk","uom":"carton","batchTrackingRequired":true,"expiryTrackingRequired":true,"qcRequiredOnReceipt":false,"shelfLifeDays":30,"isActive":true}],"totalCount":1,"page":1,"pageSize":200}""";

        private const string WarehousesPage =
            """{"items":[{"warehouseId":"a0000000-0000-0000-0000-000000000001","name":"DC Jakarta","address":"Jl","isActive":true}],"totalCount":1,"page":1,"pageSize":200}""";

        private const string LocationsPage =
            """{"items":[{"locationId":"b0000000-0000-0000-0000-000000000004","warehouseId":"a0000000-0000-0000-0000-000000000001","type":"StagingArea","code":"STG-2","isActive":true}],"totalCount":1,"page":1,"pageSize":500}""";

        private readonly List<(HttpMethod Method, string Path)> _calls = [];

        public string BacklogJson { get; set; } = "[]";

        public string WavesJson { get; set; } = "[]";

        public string PickingJson { get; set; } = "[]";

        public bool Posted(string path) =>
            _calls.Any(call => call.Method == HttpMethod.Post && string.Equals(call.Path, path, StringComparison.Ordinal));

        public bool PostedMatching(Func<string, bool> predicate) =>
            _calls.Any(call => call.Method == HttpMethod.Post && predicate(call.Path));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            _calls.Add((request.Method, path));

            var body = path switch
            {
                "/outbound/v1/outbound-orders/backlog" => BacklogJson,
                "/outbound/v1/waves" when request.Method == HttpMethod.Get => WavesJson,
                "/outbound/v1/picking-tasks" => PickingJson,
                "/masterdata/v1/products" => ProductsPage,
                "/masterdata/v1/warehouses" => WarehousesPage,
                "/masterdata/v1/locations" => LocationsPage,
                _ => "[]",
            };
            var status = request.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
