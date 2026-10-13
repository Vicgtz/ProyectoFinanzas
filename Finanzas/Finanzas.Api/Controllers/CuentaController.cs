using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CuentaController(CuentaService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<CuentaModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] bool soloActivas = false) =>
        Ok(await servicio.GetList(IdUsuario, soloActivas));

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<CuentaModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] int idCuenta) =>
        Ok(await servicio.Get(IdUsuario, idCuenta));

    [HttpGet("Tipos")]
    [ProducesResponseType(typeof(List<TipoCuentaItem>), 200)]
    public IActionResult Tipos() => Ok(TipoCuentaItem.GetList());

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] CuentaModel cuenta) =>
        Ok(await servicio.Save(IdUsuario, cuenta));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] CuentaModel cuenta) =>
        Ok(await servicio.Edit(IdUsuario, cuenta));
}
