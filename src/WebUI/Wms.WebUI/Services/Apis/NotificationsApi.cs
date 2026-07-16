namespace Wms.WebUI.Services.Apis;

// Typed client Notifications. DTO ada di NotificationsDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class NotificationsApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<PagedResult<InboxItemDto>>> GetInboxAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetPagedAsync<InboxItemDto>($"/notifications/v1/inbox?page={page}&pageSize={pageSize}", cancellationToken);
}
