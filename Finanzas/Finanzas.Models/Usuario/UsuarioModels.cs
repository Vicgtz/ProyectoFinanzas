namespace Finanzas.Models.Usuario;

public class LoginRequest
{
    public string Correo { get; set; } = "";
    public string Contra { get; set; } = "";
}

public class RegistroRequest
{
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Contra { get; set; } = "";
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
    public DateTime? Expira { get; set; }
    public int IdUsuario { get; set; }
    public string? Nombre { get; set; }
}
