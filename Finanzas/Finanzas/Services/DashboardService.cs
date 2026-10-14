using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Dashboard;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class DashboardService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Dashboard";

    public Task<BaseResponse<DashboardModel>> Get(int anio, int mes) =>
        GetData<DashboardModel>("Get", new Dictionary<string, string?> { ["anio"] = anio.ToString(), ["mes"] = mes.ToString() }, "No se pudo cargar el dashboard");
}
