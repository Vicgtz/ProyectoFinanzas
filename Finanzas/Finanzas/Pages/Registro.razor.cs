using System.ComponentModel.DataAnnotations;
using Finanzas.Models.Usuario;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Pages;

public partial class Registro
{
    [Inject] private UsuarioService Usuario { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private readonly RegistroModel modelo = new();
    private bool cargando;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        var estado = await AuthState.GetAuthenticationStateAsync();
        if (estado.User.Identity?.IsAuthenticated == true)
            Navigation.NavigateTo("/", replace: true);
    }

    private async Task CrearCuenta()
    {
        error = null;
        cargando = true;
        try
        {
            var result = await Usuario.Registrar(new RegistroRequest
            {
                Nombre = modelo.Nombre,
                Correo = modelo.Correo,
                Contra = modelo.Contra,
                Clave = modelo.Clave
            });
            if (!result.Success)
            {
                error = result.Message;
                return;
            }

            var acceso = await Usuario.LogIn(new LoginRequest { Correo = modelo.Correo, Contra = modelo.Contra });
            if (acceso.Success)
                Navigation.NavigateTo("/", replace: true);
            else
                Navigation.NavigateTo("login", replace: true);
        }
        finally
        {
            cargando = false;
        }
    }

    private class RegistroModel
    {
        [Required(ErrorMessage = "Ingresa tu nombre")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Ingresa tu correo")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        public string Correo { get; set; } = "";

        [Required(ErrorMessage = "Ingresa una contraseña")]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string Contra { get; set; } = "";

        [Required(ErrorMessage = "Confirma tu contraseña")]
        [Compare(nameof(Contra), ErrorMessage = "Las contraseñas no coinciden")]
        public string Confirmar { get; set; } = "";

        [Required(ErrorMessage = "Ingresa la clave de registro")]
        public string Clave { get; set; } = "";
    }
}
