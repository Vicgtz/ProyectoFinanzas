using Finanzas.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Finanzas.Layout;

public partial class MainLayout
{
    [Inject] private UsuarioService Usuario { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool agregarAbierto;
    private int refresco;

    private async Task IrAlContenido() => await JS.InvokeVoidAsync("finexaFoco", "contenido");

    private async Task CerrarSesion()
    {
        await Usuario.LogOut();
        Navigation.NavigateTo("login", replace: true);
    }

    // Tras guardar desde el botón global se vuelve a crear la página actual para que recargue sus datos.
    private void MovimientoGuardado()
    {
        agregarAbierto = false;
        refresco++;
    }

    private static string Inicial(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre) ? "?" : nombre.Trim()[..1].ToUpperInvariant();
}
