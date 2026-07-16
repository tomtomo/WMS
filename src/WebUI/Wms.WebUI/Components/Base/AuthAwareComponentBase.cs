using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Wms.WebUI.Components.Base;

// Base component untuk halaman yang membaca user dari cascading AuthenticationState.
// Pengisian User disimpan di sini agar tidak perlu diulang pada setiap halaman.
// Jika override OnInitializedAsync, panggil base lebih dulu supaya User sudah terisi.
public abstract class AuthAwareComponentBase : ComponentBase
{
    protected ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());

    [CascadingParameter]
    private Task<AuthenticationState>? AuthStateTask { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (AuthStateTask is not null)
        {
            User = (await AuthStateTask).User;
        }
    }
}
