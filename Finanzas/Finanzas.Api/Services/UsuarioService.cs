using System.Security.Cryptography;
using System.Text;
using Finanzas.Models.Usuario;
using Finanzas.Data;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

public class UsuarioService(FinanzasDbContext context, JwtManager jwtManager, IConfiguration configuration)
{
    private const string CredencialesInvalidas = "Correo o contraseña incorrecta";

    public async Task<AuthResponse> LogIn(LoginRequest request, string? dispositivo)
    {
        if (string.IsNullOrWhiteSpace(request.Correo)) return new AuthResponse { Message = "Correo vacío" };
        if (string.IsNullOrEmpty(request.Contra)) return new AuthResponse { Message = "Contraseña vacía" };

        var correo = request.Correo.Trim().ToLowerInvariant();
        var usuario = await context.Usuaurios.FirstOrDefaultAsync(a => a.Correo == correo);

        if (usuario == null || !PasswordHasher.Verify(request.Contra, usuario.PasswordHash))
            return new AuthResponse { Message = CredencialesInvalidas };
        if (!usuario.Activo) return new AuthResponse { Message = "Usuario inactivo" };

        var sesion = new SesionUsuario
        {
            UsuarioId = usuario.Id,
            Dispositivo = Truncar(dispositivo, 200)
        };

        var (token, expira) = jwtManager.GenerateToken(usuario.Id, sesion.Id);
        sesion.TokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        sesion.FechaExpiracion = expira;

        context.SesionUsuarios.Add(sesion);
        await context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Correcto",
            Token = token,
            Expira = expira,
            IdUsuario = usuario.Id,
            Nombre = usuario.Nombre
        };
    }

    public async Task<AuthResponse> Registrar(RegistroRequest request)
    {
        var claveEsperada = configuration["Registro:Clave"];
        if (string.IsNullOrEmpty(claveEsperada)) return new AuthResponse { Message = "El registro está deshabilitado" };
        if (!ClaveValida(request.Clave, claveEsperada)) return new AuthResponse { Message = "Clave de registro incorrecta" };

        if (string.IsNullOrWhiteSpace(request.Nombre)) return new AuthResponse { Message = "Nombre vacío" };
        if (request.Nombre.Length > 100) return new AuthResponse { Message = "Nombre mayor al permitido" };
        if (string.IsNullOrWhiteSpace(request.Correo)) return new AuthResponse { Message = "Correo vacío" };
        if (request.Correo.Length > 256 || !request.Correo.Contains('@')) return new AuthResponse { Message = "Correo inválido" };
        if (string.IsNullOrEmpty(request.Contra) || request.Contra.Length < 6) return new AuthResponse { Message = "La contraseña debe tener al menos 6 caracteres" };

        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await context.Usuaurios.AnyAsync(a => a.Correo == correo))
            return new AuthResponse { Message = "El correo ya está registrado" };

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Correo = correo,
            PasswordHash = PasswordHasher.Hash(request.Contra)
        };

        context.Usuaurios.Add(usuario);
        await context.SaveChangesAsync();

        return new AuthResponse { Success = true, Message = "Usuario guardado", IdUsuario = usuario.Id, Nombre = usuario.Nombre };
    }

    private static bool ClaveValida(string? recibida, string esperada) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(recibida ?? "")),
            SHA256.HashData(Encoding.UTF8.GetBytes(esperada)));

    private static string? Truncar(string? valor, int max) =>
        valor is { Length: > 0 } && valor.Length > max ? valor[..max] : valor;
}
