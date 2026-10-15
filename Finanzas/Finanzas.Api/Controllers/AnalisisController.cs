using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Analisis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AnalisisController(AnalisisService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<AnalisisModel>), 200)]
    public async Task<IActionResult> Get() => Ok(await servicio.Get(IdUsuario));

    // meses = 1 simula una compra al contado; más de 1, una compra a meses sin intereses.
    [HttpGet("GetImpacto")]
    [ProducesResponseType(typeof(BaseResponse<ImpactoCompraModel>), 200)]
    public async Task<IActionResult> GetImpacto([FromQuery] decimal monto, [FromQuery] int meses = 1) =>
        Ok(await servicio.GetImpacto(IdUsuario, monto, meses));
}
