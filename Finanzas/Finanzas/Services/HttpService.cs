using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

// Base de los servicios que consumen la API: un servicio por controlador (ControllerName).
// El token lo agrega AuthHttpHandler en cada petición.
public abstract class HttpService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
{
    protected ILocalStorageService Storage { get; } = storage;

    protected abstract string ControllerName { get; }

    private const string ErrorGenerico = "No se pudo completar la operación";

    protected Task<TResult?> GetAsync<TResult>(string method, IDictionary<string, string?>? query = null) =>
        Send<TResult>(new HttpRequestMessage(HttpMethod.Get, BuildUrl(method, query)));

    protected Task<TResult?> PostAsync<TResult>(string method, object? body = null, IDictionary<string, string?>? query = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(method, query));
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, jsonOptions), Encoding.UTF8, "application/json");
        return Send<TResult>(request);
    }

    // Atajos para los endpoints que responden BaseResponse / BaseResponse<T>: si falla la llamada, devuelven un mensaje.
    protected async Task<BaseResponse> PostResponse(string method, object? body = null, IDictionary<string, string?>? query = null, string error = ErrorGenerico) =>
        await PostAsync<BaseResponse>(method, body, query) ?? new BaseResponse { Message = error };

    protected async Task<BaseResponse<T>> GetData<T>(string method, IDictionary<string, string?>? query = null, string error = ErrorGenerico) =>
        await GetAsync<BaseResponse<T>>(method, query) ?? new BaseResponse<T> { Message = error };

    private string BuildUrl(string method, IDictionary<string, string?>? query)
    {
        var url = $"api/{ControllerName}/{method}";
        if (query == null) return url;

        var pares = query.Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", pares);
        return qs.Length == 0 ? url : $"{url}?{qs}";
    }

    private async Task<TResult?> Send<TResult>(HttpRequestMessage request)
    {
        try
        {
            using var response = await httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Storage.RemoveItemAsync(UsuarioService.SesionKey);
                navigation.NavigateTo("login", replace: true);
                return default;
            }

            if (!response.IsSuccessStatusCode) return default;

            return await response.Content.ReadFromJsonAsync<TResult>(jsonOptions);
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }
}
