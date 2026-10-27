using Finanzas.Api.Services;
using Finanzas.Models;
using Finanzas.Models.Recurrente;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RecurrenteController(RecurrenteService servicio) : ControllerBase
{
    private int IdUsuario => int.TryParse(User.FindFirst("IdUsuario")?.Value, out var id) ? id : 0;

    [HttpGet("GetList")]
    [ProducesResponseType(typeof(BaseResponse<List<RecurrenteModel>>), 200)]
    public async Task<IActionResult> GetList([FromQuery] bool soloActivas = false) =>
        Ok(await servicio.GetList(IdUsuario, soloActivas));

    [HttpGet("GetPendientes")]
    [ProducesResponseType(typeof(BaseResponse<List<RecurrentePendienteModel>>), 200)]
    public async Task<IActionResult> GetPendientes() =>
        Ok(await servicio.GetPendientes(IdUsuario));

    [HttpPost("Save")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Save([FromBody] RecurrenteModel modelo) =>
        Ok(await servicio.Save(IdUsuario, modelo));

    [HttpPost("Edit")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Edit([FromBody] RecurrenteModel modelo) =>
        Ok(await servicio.Edit(IdUsuario, modelo));

    [HttpPost("Delete")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Delete([FromQuery] int idRecurrente) =>
        Ok(await servicio.Delete(IdUsuario, idRecurrente));

    [HttpPost("Confirmar")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarRecurrenteRequest request) =>
        Ok(await servicio.Confirmar(IdUsuario, request));

    [HttpPost("Omitir")]
    [ProducesResponseType(typeof(BaseResponse), 200)]
    public async Task<IActionResult> Omitir([FromBody] OmitirRecurrenteRequest request) =>
        Ok(await servicio.Omitir(IdUsuario, request));
}
