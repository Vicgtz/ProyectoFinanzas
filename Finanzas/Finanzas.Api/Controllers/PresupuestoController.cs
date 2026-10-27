using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Presupuesto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PresupuestoController(PresupuestoService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetResumen")]
    [ProducesResponseType(typeof(BaseResponse<PresupuestoResumenModel>), 200)]
    public async Task<IActionResult> GetResumen([FromQuery] int? anio, [FromQuery] int? mes) =>
        Ok(await servicio.GetResumen(IdUsuario, anio, mes));

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] PresupuestoModel modelo) =>
        Ok(await servicio.Save(IdUsuario, modelo));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] PresupuestoModel modelo) =>
        Ok(await servicio.Edit(IdUsuario, modelo));

    [HttpPost("Delete")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Delete([FromQuery] int idPresupuesto) =>
        Ok(await servicio.Delete(IdUsuario, idPresupuesto));
}
