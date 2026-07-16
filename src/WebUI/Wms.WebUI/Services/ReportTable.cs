using MudBlazor;

namespace Wms.WebUI.Services;

// Ubah hasil API berpaginasi menjadi data tabel. Jika gagal load, tampilkan error agar tidak terlihat seperti laporan kosong.
public static class ReportTable
{
    public static TableData<T> ToTableData<T>(this ApiResult<PagedResult<T>> result, ISnackbar snackbar)
    {
        ArgumentNullException.ThrowIfNull(snackbar);

        if (result.Success)
        {
            return new TableData<T> { Items = result.Value!.Items, TotalItems = result.Value.TotalCount };
        }

        snackbar.Add($"Gagal memuat laporan: {result.ErrorMessage}", Severity.Error);
        return new TableData<T> { Items = [], TotalItems = 0 };
    }
}
