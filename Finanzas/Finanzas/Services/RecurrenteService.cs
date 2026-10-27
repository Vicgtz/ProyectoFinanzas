using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Recurrente;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class RecurrenteService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Recurrente";

    public Task<BaseResponse<List<RecurrenteModel>>> GetList(bool soloActivas = false) =>
        GetData<List<RecurrenteModel>>("GetList", new Dictionary<string, string?> { ["soloActivas"] = soloActivas.ToString() }, "No se pudieron cargar las recurrentes");

    public Task<BaseResponse<List<RecurrentePendienteModel>>> GetPendientes() =>
        GetData<List<RecurrentePendienteModel>>("GetPendientes", error: "No se pudieron cargar los pendientes");

    public Task<BaseResponse> Save(RecurrenteModel modelo) =>
        PostResponse("Save", modelo, error: "Recurrente no guardada");

    public Task<BaseResponse> Edit(RecurrenteModel modelo) =>
        PostResponse("Edit", modelo, error: "Recurrente no guardada");

    public Task<BaseResponse> Delete(int idRecurrente) =>
        PostResponse("Delete", query: new Dictionary<string, string?> { ["idRecurrente"] = idRecurrente.ToString() }, error: "Recurrente no eliminada");

    public Task<BaseResponse> Confirmar(ConfirmarRecurrenteRequest request) =>
        PostResponse("Confirmar", request, error: "No se pudo confirmar");

    public Task<BaseResponse> Omitir(OmitirRecurrenteRequest request) =>
        PostResponse("Omitir", request, error: "No se pudo omitir");
}
