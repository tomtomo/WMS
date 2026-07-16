using System.Net;
using System.Security.Claims;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Wms.WebUI.Components.Pages.Inbound;
using Wms.WebUI.Services;
using Xunit;

namespace Wms.WebUI.IntegrationTests;

// Test GoodsReceiptDetail beserta lampirannya melalui route GoodsReceiptId dan typed client.
public sealed class GoodsReceiptDetailTests : TestContext
{
    private const string UploadGRAttachment = "Inbound.UploadGRAttachment";
    private const string DeleteGRAttachment = "Inbound.DeleteGRAttachment";

    private static readonly Guid GrId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly string GrJson =
        """{"goodsReceiptId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","poRef":"PO-TEST-1","supplierId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","warehouseId":"a0000000-0000-0000-0000-000000000001","dockDoor":"D-1","status":"Confirmed","holdReason":null,"expectedLines":[{"sku":"SKU-MILK","expectedQty":100,"uom":"CARTON"}],"scannedLines":[],"discrepancies":[]}""";

    private static readonly string AttachmentsJson =
        """[{"attachmentId":"cccccccc-cccc-cccc-cccc-cccccccccccc","goodsReceiptId":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","fileName":"invoice.pdf","contentType":"application/pdf","sizeBytes":12345,"uploadedAt":"2026-07-15T10:00:00+00:00","isActive":true}]""";

    private static readonly string EmptyPage = """{"items":[],"totalCount":0,"page":1,"pageSize":10}""";

    public GoodsReceiptDetailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
        Services.AddWmsApiClients();
    }

    [Fact]
    public void Renders_goods_receipt_detail_with_expected_lines()
    {
        Authorize();

        var cut = RenderDetail();

        cut.Markup.Should().Contain("Goods Receipt Detail");
        cut.Markup.Should().Contain("PO-TEST-1");
        cut.Markup.Should().Contain("SKU-MILK");
    }

    [Fact]
    public void Renders_attachment_row_from_list()
    {
        Authorize();

        var cut = RenderDetail();

        cut.Markup.Should().Contain("Attachments");
        cut.Markup.Should().Contain("invoice.pdf");
    }

    [Fact]
    public void Upload_button_shown_only_with_permission()
    {
        Authorize(UploadGRAttachment);

        HasButton(RenderDetail(), "Upload").Should().BeTrue();
    }

    [Fact]
    public void Upload_button_hidden_without_permission()
    {
        Authorize();

        HasButton(RenderDetail(), "Upload").Should().BeFalse();
    }

    [Fact]
    public void Delete_button_shown_only_with_permission()
    {
        Authorize(DeleteGRAttachment);

        HasDeleteButton(RenderDetail()).Should().BeTrue();
    }

    [Fact]
    public void Delete_button_hidden_without_permission()
    {
        Authorize();

        HasDeleteButton(RenderDetail()).Should().BeFalse();
    }

    private static bool HasButton(IRenderedFragment cut, string text) =>
        cut.FindAll("button").Any(button => button.TextContent.Contains(text, StringComparison.Ordinal));

    private static bool HasDeleteButton(IRenderedFragment cut) =>
        cut.FindAll("button[aria-label=\"Delete attachment\"]").Count > 0;

    private IRenderedComponent<GoodsReceiptDetail> RenderDetail() =>
        RenderComponent<GoodsReceiptDetail>(parameters => parameters.Add(page => page.GoodsReceiptId, GrId));

    private void Authorize(params string[] permissions)
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("op");
        auth.SetClaims([.. permissions.Select(permission => new Claim("permission", permission))]);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler()) { BaseAddress = new Uri("http://localhost") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = ResolveBody(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        private static string ResolveBody(string path)
        {
            if (path.EndsWith("/attachments", StringComparison.Ordinal))
            {
                return AttachmentsJson;
            }

            return path.StartsWith("/inbound/v1/goods-receipts/", StringComparison.Ordinal) ? GrJson : EmptyPage;
        }
    }
}
