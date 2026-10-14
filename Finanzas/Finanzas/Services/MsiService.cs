using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Msi;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class MsiService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Msi";

    public Task<BaseResponse<MsiResumenModel>> GetResumen() =>
        GetData<MsiResumenModel>("GetResumen", error: "No se pudieron cargar las compras a meses sin intereses");
}
