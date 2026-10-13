using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Operacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OperacionController(OperacionService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<OperacionModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] OperacionFiltro filtro) =>
        Ok(await servicio.GetList(IdUsuario, filtro));

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<OperacionModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] long idOperacion) =>
        Ok(await servicio.Get(IdUsuario, idOperacion));

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] OperacionRequest request) =>
        Ok(await servicio.Save(IdUsuario, request));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] OperacionRequest request) =>
        Ok(await servicio.Edit(IdUsuario, request));

    [HttpPost("Delete")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Delete([FromQuery] long idOperacion) =>
        Ok(await servicio.Delete(IdUsuario, idOperacion));
}
