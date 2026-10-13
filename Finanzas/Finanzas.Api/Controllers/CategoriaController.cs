using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Categoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriaController(CategoriaService servicio) : ControllerBase
{
    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<CategoriaModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] string? tipoSugerido = null) =>
        Ok(await servicio.GetList(tipoSugerido));

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<CategoriaModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] int idCategoria) =>
        Ok(await servicio.Get(idCategoria));
}
