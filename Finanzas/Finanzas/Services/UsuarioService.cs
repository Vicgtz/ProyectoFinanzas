using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models.Usuario;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Services;

public class UsuarioService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions, AuthenticationStateProvider authProvider)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    public const string SesionKey = "sesion";

    protected override string ControllerName => "Usuario";

    public async Task<AuthResponse> LogIn(LoginRequest request)
    {
        var result = await PostAsync<AuthResponse>("Login", request)
            ?? new AuthResponse { Message = "No se pudo conectar con el servidor" };

        if (result.Success && !string.IsNullOrEmpty(result.Token))
        {
            await Storage.SetItemAsync(SesionKey, result);
            ((CustomAuthProvider)authProvider).NotificarCambio();
        }
        return result;
    }

    public async Task<AuthResponse> Registrar(RegistroRequest request) =>
        await PostAsync<AuthResponse>("Registrar", request)
            ?? new AuthResponse { Message = "No se pudo conectar con el servidor" };

    public async Task LogOut()
    {
        await Storage.RemoveItemAsync(SesionKey);
        ((CustomAuthProvider)authProvider).NotificarCambio();
    }
}
