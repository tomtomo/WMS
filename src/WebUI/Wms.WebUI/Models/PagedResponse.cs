namespace Wms.WebUI.Models;

// Bentuk paging read-model dari gateway (PagedResult) untuk konsumsi WebUI.
public sealed record PagedResponse<T>(List<T> Items, int Total, int Page, int PageSize);
