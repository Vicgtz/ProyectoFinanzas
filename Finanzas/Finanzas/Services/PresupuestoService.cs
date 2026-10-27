using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Presupuesto;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class PresupuestoService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Presupuesto";

    public Task<BaseResponse<PresupuestoResumenModel>> GetResumen(int? anio = null, int? mes = null) =>
        GetData<PresupuestoResumenModel>("GetResumen", new Dictionary<string, string?> { ["anio"] = anio?.ToString(), ["mes"] = mes?.ToString() }, "No se pudieron cargar los presupuestos");

    public Task<BaseResponse> Save(PresupuestoModel modelo) =>
        PostResponse("Save", modelo, error: "Presupuesto no guardado");

    public Task<BaseResponse> Edit(PresupuestoModel modelo) =>
        PostResponse("Edit", modelo, error: "Presupuesto no guardado");

    public Task<BaseResponse> Delete(int idPresupuesto) =>
        PostResponse("Delete", query: new Dictionary<string, string?> { ["idPresupuesto"] = idPresupuesto.ToString() }, error: "Presupuesto no eliminado");
}
