namespace Wms.WebUI.Services.Apis;

// Typed client MasterData. DTO ada di MasterDataDtos.cs (namespace induk Wms.WebUI.Services).
public sealed class MasterDataApi(IHttpClientFactory httpClientFactory) : ApiClientBase(httpClientFactory)
{
    public Task<ApiResult<PagedResult<ProductDto>>> ListProductsAsync(
        int page, int pageSize, bool includeInactive, CancellationToken cancellationToken = default) =>
        GetPagedAsync<ProductDto>(
            $"/masterdata/v1/products?page={page}&pageSize={pageSize}&includeInactive={Bool(includeInactive)}", cancellationToken);

    public Task<ApiResult> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync("/masterdata/v1/products", request, cancellationToken);

    public Task<ApiResult> UpdateProductAsync(string sku, UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync($"/masterdata/v1/products/{Uri.EscapeDataString(sku)}", request, cancellationToken);

    public Task<ApiResult> DeactivateProductAsync(string sku, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/masterdata/v1/products/{Uri.EscapeDataString(sku)}", cancellationToken);

    public Task<ApiResult<PagedResult<WarehouseDto>>> ListWarehousesAsync(
        int page, int pageSize, bool includeInactive, CancellationToken cancellationToken = default) =>
        GetPagedAsync<WarehouseDto>(
            $"/masterdata/v1/warehouses?page={page}&pageSize={pageSize}&includeInactive={Bool(includeInactive)}", cancellationToken);

    public Task<ApiResult> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync("/masterdata/v1/warehouses", request, cancellationToken);

    public Task<ApiResult> UpdateWarehouseAsync(Guid warehouseId, UpdateWarehouseRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync($"/masterdata/v1/warehouses/{warehouseId}", request, cancellationToken);

    public Task<ApiResult> DeactivateWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/masterdata/v1/warehouses/{warehouseId}", cancellationToken);

    public Task<ApiResult<PagedResult<LocationDto>>> ListLocationsAsync(
        int page, int pageSize, bool includeInactive, CancellationToken cancellationToken = default) =>
        GetPagedAsync<LocationDto>(
            $"/masterdata/v1/locations?page={page}&pageSize={pageSize}&includeInactive={Bool(includeInactive)}", cancellationToken);

    public Task<ApiResult> CreateLocationAsync(CreateLocationRequest request, CancellationToken cancellationToken = default) =>
        PostJsonAsync("/masterdata/v1/locations", request, cancellationToken);

    public Task<ApiResult> UpdateLocationAsync(Guid locationId, UpdateLocationRequest request, CancellationToken cancellationToken = default) =>
        PutJsonAsync($"/masterdata/v1/locations/{locationId}", request, cancellationToken);

    public Task<ApiResult> DeactivateLocationAsync(Guid locationId, CancellationToken cancellationToken = default) =>
        DeleteAsync($"/masterdata/v1/locations/{locationId}", cancellationToken);

    // Query bool lowercase supaya URL bersih
    private static string Bool(bool value) => value ? "true" : "false";
}
