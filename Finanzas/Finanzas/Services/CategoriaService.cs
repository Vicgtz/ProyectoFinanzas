using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Categoria;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class CategoriaService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Categoria";

    public Task<BaseResponse<List<CategoriaModel>>> GetList(string? tipoSugerido = null, bool incluirInactivas = false) =>
        GetData<List<CategoriaModel>>("GetList", new Dictionary<string, string?> { ["tipoSugerido"] = tipoSugerido, ["incluirInactivas"] = incluirInactivas ? "true" : null }, "No se pudieron cargar las categorías");

    public Task<BaseResponse<CategoriaModel>> Get(int idCategoria) =>
        GetData<CategoriaModel>("Get", new Dictionary<string, string?> { ["idCategoria"] = idCategoria.ToString() }, "No se pudo cargar la categoría");

    public Task<BaseResponse> Save(CategoriaModel modelo) =>
        PostResponse("Save", modelo, error: "Categoría no guardada");

    public Task<BaseResponse> Edit(CategoriaModel modelo) =>
        PostResponse("Edit", modelo, error: "Categoría no guardada");
}
