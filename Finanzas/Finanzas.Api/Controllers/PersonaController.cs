using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Persona;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PersonaController(PersonaService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<PersonaModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] bool soloActivas = false) =>
        Ok(await servicio.GetList(IdUsuario, soloActivas));

    [HttpGet("Get")]
    [ProducesResponseType(typeof(BaseResponse<PersonaModel>), 200)]
    public async Task<IActionResult> Get([FromQuery] int idPersona) =>
        Ok(await servicio.Get(IdUsuario, idPersona));

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] PersonaModel persona) =>
        Ok(await servicio.Save(IdUsuario, persona));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] PersonaModel persona) =>
        Ok(await servicio.Edit(IdUsuario, persona));
}
