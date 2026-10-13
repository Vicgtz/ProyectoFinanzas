using Finanzas.Models.Usuario;
using Finanzas.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finanzas.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController(UsuarioService servicio) : ControllerBase
{
    [HttpPost("Login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var dispositivo = Request.Headers.UserAgent.ToString();
        var result = await servicio.LogIn(request, dispositivo);
        return Ok(result);
    }

    [HttpPost("Registrar")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest request)
    {
        var result = await servicio.Registrar(request);
        return Ok(result);
    }
}
