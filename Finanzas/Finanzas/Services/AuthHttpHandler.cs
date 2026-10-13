using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Finanzas.Models.Usuario;

namespace Finanzas.Services;

// Agrega el token de la sesión guardada a cada petición hacia la API.
public class AuthHttpHandler(ILocalStorageService storage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sesion = await storage.GetItemAsync<AuthResponse>(UsuarioService.SesionKey, cancellationToken);
        if (!string.IsNullOrEmpty(sesion?.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}
