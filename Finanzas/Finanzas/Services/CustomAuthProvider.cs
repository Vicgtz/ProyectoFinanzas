using System.Security.Claims;
using Blazored.LocalStorage;
using Finanzas.Models.Usuario;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Services;

public class CustomAuthProvider(ILocalStorageService storage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var sesion = await storage.GetItemAsync<AuthResponse>(UsuarioService.SesionKey);
        if (sesion == null || string.IsNullOrEmpty(sesion.Token)) return Anonimo;

        if (sesion.Expira == null || sesion.Expira.Value.ToUniversalTime() <= DateTime.UtcNow)
        {
            await storage.RemoveItemAsync(UsuarioService.SesionKey);
            return Anonimo;
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, sesion.Nombre ?? ""),
            new Claim("IdUsuario", sesion.IdUsuario.ToString())
        ], "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotificarCambio() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
