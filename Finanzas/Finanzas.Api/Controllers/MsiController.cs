using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Msi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MsiController(MsiService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetResumen")]
    [ProducesResponseType(typeof(BaseResponse<MsiResumenModel>), 200)]
    public async Task<IActionResult> GetResumen() => Ok(await servicio.GetResumen(IdUsuario));
}
