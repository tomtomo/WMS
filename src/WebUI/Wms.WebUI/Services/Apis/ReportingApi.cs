namespace Wms.WebUI.Services.Apis;

// Typed client Reporting. DTO ada di ReportingDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class ReportingApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<PagedResult<StockOnHandRow>>> StockOnHandAsync(
        int page, int pageSize, Guid? warehouseId = null, string? sku = null, CancellationToken cancellationToken = default) =>
        GetPagedAsync<StockOnHandRow>(
            $"/reporting/v1/reports/stock-on-hand?page={page}&pageSize={pageSize}{Filter("warehouseId", warehouseId)}{Filter("sku", sku)}", cancellationToken);

    public Task<ApiResult<PagedResult<DispatchSummaryRow>>> DispatchSummaryAsync(
        int page, int pageSize, Guid? warehouseId = null, CancellationToken cancellationToken = default) =>
        GetPagedAsync<DispatchSummaryRow>(
            $"/reporting/v1/reports/dispatch-summary?page={page}&pageSize={pageSize}{Filter("warehouseId", warehouseId)}", cancellationToken);

    public Task<ApiResult<PagedResult<OperatorProductivityRow>>> OperatorProductivityAsync(
        int page, int pageSize, Guid? operatorId = null, CancellationToken cancellationToken = default) =>
        GetPagedAsync<OperatorProductivityRow>(
            $"/reporting/v1/reports/operator-productivity?page={page}&pageSize={pageSize}{Filter("operatorId", operatorId)}", cancellationToken);

    public Task<ApiResult<PagedResult<SupplierPerformanceRow>>> SupplierPerformanceAsync(
        int page, int pageSize, Guid? supplierId = null, CancellationToken cancellationToken = default) =>
        GetPagedAsync<SupplierPerformanceRow>(
            $"/reporting/v1/reports/supplier-performance?page={page}&pageSize={pageSize}{Filter("supplierId", supplierId)}", cancellationToken);

    // Query filter opsional — kosong bila null supaya URL bersih.
    private static string Filter(string name, object? value) =>
        value is null ? string.Empty : $"&{name}={Uri.EscapeDataString(value.ToString()!)}";
}
