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

    public Task<BaseResponse<List<CategoriaModel>>> GetList(string? tipoSugerido = null) =>
        GetData<List<CategoriaModel>>("GetList", new Dictionary<string, string?> { ["tipoSugerido"] = tipoSugerido }, "No se pudieron cargar las categorías");

    public Task<BaseResponse<CategoriaModel>> Get(int idCategoria) =>
        GetData<CategoriaModel>("Get", new Dictionary<string, string?> { ["idCategoria"] = idCategoria.ToString() }, "No se pudo cargar la categoría");
}
