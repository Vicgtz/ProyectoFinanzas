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
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<CategoriaModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] string? tipoSugerido = null, [FromQuery] bool incluirInactivas = false) =>
        Ok(await servicio.GetList(IdUsuario, tipoSugerido, incluirInactivas));

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<CategoriaModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] int idCategoria) =>
        Ok(await servicio.Get(IdUsuario, idCategoria));

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] CategoriaModel modelo) =>
        Ok(await servicio.Save(IdUsuario, modelo));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] CategoriaModel modelo) =>
        Ok(await servicio.Edit(IdUsuario, modelo));
}
