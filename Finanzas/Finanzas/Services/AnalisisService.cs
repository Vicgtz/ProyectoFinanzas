using System.Globalization;
using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Analisis;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class AnalisisService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Analisis";

    public Task<BaseResponse<AnalisisModel>> Get() =>
        GetData<AnalisisModel>("Get", error: "No se pudo cargar el análisis");

    public Task<BaseResponse<ImpactoCompraModel>> GetImpacto(decimal monto, int meses) =>
        GetData<ImpactoCompraModel>("GetImpacto", new Dictionary<string, string?>
        {
            ["monto"] = monto.ToString(CultureInfo.InvariantCulture),
            ["meses"] = meses.ToString()
        }, "No se pudo calcular el impacto de la compra");
}
