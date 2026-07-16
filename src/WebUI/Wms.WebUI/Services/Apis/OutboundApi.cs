namespace Wms.WebUI.Services.Apis;

// Typed client Outbound. DTO ada di OutboundDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class OutboundApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<WaveDetailDto>> GetWaveAsync(Guid waveId, CancellationToken cancellationToken = default) =>
        GetOneAsync<WaveDetailDto>($"/outbound/v1/waves/{waveId}", cancellationToken);

    public Task<ApiResult<OutboundOrderDetailDto>> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        GetOneAsync<OutboundOrderDetailDto>($"/outbound/v1/outbound-orders/{orderId}", cancellationToken);

    public Task<ApiResult<IReadOnlyList<OrderBacklogDto>>> GetBacklogAsync(CancellationToken cancellationToken = default) =>
        GetListAsync<OrderBacklogDto>("/outbound/v1/outbound-orders/backlog", cancellationToken);

    public Task<ApiResult<IReadOnlyList<WaveListItemDto>>> ListWavesAsync(Guid warehouseId, string status, CancellationToken cancellationToken = default) =>
        GetListAsync<WaveListItemDto>($"/outbound/v1/waves?warehouseId={warehouseId}&status={status}", cancellationToken);

    public Task<ApiResult<IReadOnlyList<PickingTaskDto>>> ListPickingTasksAsync(Guid? assignedTo, CancellationToken cancellationToken = default) =>
        GetListAsync<PickingTaskDto>(
            assignedTo is null ? "/outbound/v1/picking-tasks" : $"/outbound/v1/picking-tasks?assignedTo={assignedTo}", cancellationToken);

    public Task<ApiResult> CreateOrderAsync(CreateOutboundOrderRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync("/outbound/v1/outbound-orders", request, cancellationToken);

    public Task<ApiResult> CreateWaveAsync(CreateWaveRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync("/outbound/v1/waves", request, cancellationToken);

    public Task<ApiResult> DispatchWaveAsync(Guid waveId, CancellationToken cancellationToken = default) =>
        PostEmptyAsync($"/outbound/v1/waves/{waveId}/dispatch", cancellationToken);

    public Task<ApiResult> CompletePickingAsync(Guid pickingTaskId, CompletePickingRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync($"/outbound/v1/picking-tasks/{pickingTaskId}/complete", request, cancellationToken);
}
