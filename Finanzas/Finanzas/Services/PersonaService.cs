using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Persona;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class PersonaService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Persona";

    public Task<BaseResponse<List<PersonaModel>>> GetList(bool soloActivas = false) =>
        GetData<List<PersonaModel>>("GetList", new Dictionary<string, string?> { ["soloActivas"] = soloActivas.ToString() }, "No se pudieron cargar las personas");

    public Task<BaseResponse<PersonaModel>> Get(int idPersona) =>
        GetData<PersonaModel>("Get", new Dictionary<string, string?> { ["idPersona"] = idPersona.ToString() }, "No se pudo cargar la persona");

    public Task<BaseResponse> Save(PersonaModel persona) =>
        PostResponse("Save", persona, error: "Persona no guardada");

    public Task<BaseResponse> Edit(PersonaModel persona) =>
        PostResponse("Edit", persona, error: "Persona no guardada");
}
