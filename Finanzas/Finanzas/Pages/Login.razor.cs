using System.ComponentModel.DataAnnotations;
using Finanzas.Models.Usuario;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Pages;

public partial class Login
{
    [Inject] private UsuarioService Usuario { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly LoginModel modelo = new();
    private bool cargando;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        var estado = await AuthState.GetAuthenticationStateAsync();
        if (estado.User.Identity?.IsAuthenticated == true)
            Navigation.NavigateTo("/", replace: true);
    }

    private async Task IniciarSesion()
    {
        error = null;
        cargando = true;
        try
        {
            var result = await Usuario.LogIn(new LoginRequest { Correo = modelo.Correo, Contra = modelo.Contra });
            if (result.Success)
                Navigation.NavigateTo("/", replace: true);
            else
                error = result.Message;
        }
        finally
        {
            cargando = false;
        }
    }

    private class LoginModel
    {
        [Required(ErrorMessage = "Ingresa tu correo")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingresa tu contraseña")]
        public string Contra { get; set; } = "";
    }
}
