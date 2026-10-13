using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Layout;

public partial class MainLayout
{
    [Inject] private UsuarioService Usuario { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private async Task CerrarSesion()
    {
        await Usuario.LogOut();
        Navigation.NavigateTo("login", replace: true);
    }

    private static string Inicial(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "?" : nombre.Trim()[..1].ToUpperInvariant();
}
