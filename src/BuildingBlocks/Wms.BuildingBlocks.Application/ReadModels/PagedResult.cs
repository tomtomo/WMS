namespace Wms.BuildingBlocks.Application.ReadModels;

// Reader paged: Items read only dan total untuk paging UI.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
