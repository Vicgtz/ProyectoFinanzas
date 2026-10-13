using System.Globalization;
using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Operacion;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class OperacionService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Operacion";

    public Task<BaseResponse<List<OperacionModel>>> GetList(OperacionFiltro? filtro = null)
    {
        filtro ??= new OperacionFiltro();
        var query = new Dictionary<string, string?>
        {
            ["desde"] = filtro.Desde?.ToString("s", CultureInfo.InvariantCulture),
            ["hasta"] = filtro.Hasta?.ToString("s", CultureInfo.InvariantCulture),
            ["tipoOperacion"] = filtro.TipoOperacion,
            ["cuentaId"] = filtro.CuentaId?.ToString(),
            ["categoriaId"] = filtro.CategoriaId?.ToString(),
            ["skip"] = filtro.Skip.ToString(),
            ["take"] = filtro.Take.ToString()
        };
        return GetData<List<OperacionModel>>("GetList", query, "No se pudieron cargar las operaciones");
    }

    public Task<BaseResponse<OperacionModel>> Get(long idOperacion) =>
        GetData<OperacionModel>("Get", new Dictionary<string, string?> { ["idOperacion"] = idOperacion.ToString() }, "No se pudo cargar la operación");

    public Task<BaseResponse> Save(OperacionRequest operacion) =>
        PostResponse("Save", operacion, error: "Operación no guardada");

    public Task<BaseResponse> Edit(OperacionRequest operacion) =>
        PostResponse("Edit", operacion, error: "Operación no guardada");

    public Task<BaseResponse> Delete(long idOperacion) =>
        PostResponse("Delete", query: new Dictionary<string, string?> { ["idOperacion"] = idOperacion.ToString() }, error: "Operación no eliminada");
}
