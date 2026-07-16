namespace Wms.WebUI.Services.Apis;

// Typed client Inventory. DTO ada di InventoryDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class InventoryApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<IReadOnlyList<PutawayTaskDto>>> ListPutawayTasksAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<PutawayTaskDto>("/inventory/v1/putaway-tasks", cancellationToken);

    public Task<ApiResult> CompletePutawayAsync(Guid putawayTaskId, CompletePutawayRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/inventory/v1/putaway-tasks/{putawayTaskId}/complete", request, cancellationToken);
}
