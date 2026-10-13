using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Services;

public class CuentaService(HttpClient httpClient, NavigationManager navigation, ILocalStorageService storage, JsonSerializerOptions jsonOptions)
    : HttpService(httpClient, navigation, storage, jsonOptions)
{
    protected override string ControllerName => "Cuenta";

    public Task<BaseResponse<List<CuentaModel>>> GetList(bool soloActivas = false) =>
        GetData<List<CuentaModel>>("GetList", new Dictionary<string, string?> { ["soloActivas"] = soloActivas.ToString() }, "No se pudieron cargar las cuentas");

    public Task<BaseResponse<CuentaModel>> Get(int idCuenta) =>
        GetData<CuentaModel>("Get", new Dictionary<string, string?> { ["idCuenta"] = idCuenta.ToString() }, "No se pudo cargar la cuenta");

    public Task<BaseResponse> Save(CuentaModel cuenta) =>
        PostResponse("Save", cuenta, error: "Cuenta no guardada");

    public Task<BaseResponse> Edit(CuentaModel cuenta) =>
        PostResponse("Edit", cuenta, error: "Cuenta no guardada");
}
