using Microsoft.AspNetCore.Components;
using MudBlazor;
using Wms.WebUI.Components.Shared;
using Wms.WebUI.Services;

namespace Wms.WebUI.Components.Base;

// Base untuk halaman CRUD Product, Warehouse, dan Location yang punya alur sama.
// Alurnya meliputi tabel berpaginasi, filter data nonaktif, dialog tambah, dan konfirmasi nonaktifkan.
// Halaman turunan cukup menyediakan tabel dan implementasi member abstrak di bawah.
public abstract class MasterDataCrudPageBase<TDto> : AuthAwareComponentBase
{
    [Inject]
    protected WmsApiClient Api { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = default!;

    protected MudTable<TDto>? Table { get; set; }

    protected bool ShowInactive { get; set; }

    // Komponen dialog untuk form tambah data.
    protected abstract Type FormDialogType { get; }

    // Nama entitas untuk judul dan pesan, misalnya "Product".
    protected abstract string EntityLabel { get; }

    // Sumber data halaman yang sudah berpaginasi.
    protected abstract Task<ApiResult<PagedResult<TDto>>> ListAsync(int page, int pageSize, bool includeInactive, CancellationToken cancellationToken);

    // Teks pengenal baris, misalnya SKU, nama, atau kode.
    protected abstract string Describe(TDto row);

    protected abstract Task<ApiResult> DeactivateCoreAsync(TDto row);

    // ToTableData menampilkan error ke snackbar agar kegagalan tidak terlihat seperti tabel kosong.
    protected async Task<TableData<TDto>> LoadAsync(TableState state, CancellationToken cancellationToken)
    {
        var result = await ListAsync(state.Page + 1, state.PageSize, ShowInactive, cancellationToken);
        return result.ToTableData(Snackbar);
    }

    protected void Reload() => Table?.ReloadServerData();

    protected async Task OpenCreateAsync()
    {
        var dialog = await DialogService.ShowAsync(FormDialogType, $"Tambah {EntityLabel}");
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            Table?.ReloadServerData();
        }
    }

    protected async Task DeactivateAsync(TDto row)
    {
        var label = Describe(row);
        var parameters = new DialogParameters
        {
            [nameof(ConfirmDialog.Message)] = $"Deactivate {EntityLabel.ToLowerInvariant()} {label}? (soft-delete)",
            [nameof(ConfirmDialog.ConfirmText)] = "Deactivate",
            [nameof(ConfirmDialog.ConfirmColor)] = Color.Error,
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>($"Deactivate {label}", parameters);
        var confirmed = await dialog.Result;
        if (confirmed is { Canceled: false, Data: true })
        {
            var result = await DeactivateCoreAsync(row);
            Snackbar.Add(
                result.Success ? $"{EntityLabel} di-deactivate." : $"Gagal: {result.ErrorMessage}",
                result.Success ? Severity.Success : Severity.Error);
            if (result.Success)
            {
                Table?.ReloadServerData();
            }
        }
    }
}
