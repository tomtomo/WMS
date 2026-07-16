using System.Net.Http.Headers;

namespace Wms.WebUI.Services.Apis;

// Typed client untuk modul Inbound. DTO ada di InboundDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class InboundApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<GoodsReceiptDetailDto>> GetGoodsReceiptAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default) =>
        GetOneAsync<GoodsReceiptDetailDto>($"/inbound/v1/goods-receipts/{goodsReceiptId}", cancellationToken);

    public Task<ApiResult<PagedResult<GoodsReceiptListItemDto>>> ListPendingAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<GoodsReceiptListItemDto>($"/inbound/v1/goods-receipts/pending?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<ApiResult<CreatedGoodsReceiptResponse>> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default) =>
        PostReadAsync<CreatedGoodsReceiptResponse>("/inbound/v1/goods-receipts", request, cancellationToken);

    public Task<ApiResult> ScanAsync(Guid goodsReceiptId, ScanLineRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/inbound/v1/goods-receipts/{goodsReceiptId}/scans", request, cancellationToken);

    public Task<ApiResult> CompleteScanAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default) =>
        PostEmptyAsync($"/inbound/v1/goods-receipts/{goodsReceiptId}/complete-scan", cancellationToken);

    public Task<ApiResult> ConfirmAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default) =>
        PostEmptyAsync($"/inbound/v1/goods-receipts/{goodsReceiptId}/confirm", cancellationToken);

    public Task<ApiResult<IReadOnlyList<GRAttachmentDto>>> ListAttachmentsAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default) =>
        GetListAsync<GRAttachmentDto>($"/inbound/v1/goods-receipts/{goodsReceiptId}/attachments", cancellationToken);

    public Task<ApiResult<GRAttachmentDownloadUrl>> GetAttachmentDownloadUrlAsync(Guid goodsReceiptId, Guid attachmentId, CancellationToken cancellationToken = default) =>
        GetOneAsync<GRAttachmentDownloadUrl>($"/inbound/v1/goods-receipts/{goodsReceiptId}/attachments/{attachmentId}/download-url", cancellationToken);

    public Task<ApiResult> DeleteAttachmentAsync(Guid goodsReceiptId, Guid attachmentId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/inbound/v1/goods-receipts/{goodsReceiptId}/attachments/{attachmentId}", cancellationToken);

    // Upload multipart: satu part bernama "file" sesuai parameter IFormFile di endpoint.
    public async Task<ApiResult<GRAttachmentUploadedDto>> UploadAttachmentAsync(
        Guid goodsReceiptId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        form.Add(fileContent, "file", fileName);
        return await PostContentReadAsync<GRAttachmentUploadedDto>(
            $"/inbound/v1/goods-receipts/{goodsReceiptId}/attachments", form, cancellationToken);
    }
}
