using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController(DashboardService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<DashboardModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] int? anio, [FromQuery] int? mes) =>
        Ok(await servicio.Get(IdUsuario, anio, mes));
}
