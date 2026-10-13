using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finanzas.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Finanzas.Api.Services;

public class JwtManager(IOptions<JwtSettings> options)
{
    private readonly JwtSettings settings = options.Value;

    public (string Token, DateTime Expira) GenerateToken(int idUsuario, Guid idSesion)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(settings.TokenExpiryMinutes);

        var claims = new[]
        {
            new Claim("IdUsuario", idUsuario.ToString()),
            new Claim("IdSesion", idSesion.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
