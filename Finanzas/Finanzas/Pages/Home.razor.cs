using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Pages;

public partial class Home
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private string Nombre { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        var estado = await AuthState.GetAuthenticationStateAsync();
        Nombre = estado.User.Identity?.Name ?? "";
    }
}
