using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Operacion;
using Finanzas.Models.Persona;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

public class PersonaService(FinanzasDbContext context)
{
    public async Task<BaseResponse<PersonaModel>> Get(int idUsuario, int idPersona)
    {
        var persona = await context.Personas.AsNoTracking()
            .Where(a => a.Id == idPersona && a.UsuarioId == idUsuario)
            .Select(a => ToModel(a))
            .FirstOrDefaultAsync();

        if (persona == null) return new BaseResponse<PersonaModel> { Message = "Persona no encontrada" };

        var saldos = await Saldos(idUsuario, idPersona);
        if (saldos.TryGetValue(persona.Id, out var s)) (persona.PorCobrar, persona.PorPagar) = s;
        return new BaseResponse<PersonaModel> { Success = true, Data = persona };
    }

    public async Task<BaseResponse<List<PersonaModel>>> GetList(int idUsuario, bool soloActivas = false)
    {
        var personas = await context.Personas.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && (!soloActivas || a.Activa))
            .OrderBy(a => a.Nombre)
            .Select(a => ToModel(a))
            .ToListAsync();

        var saldos = await Saldos(idUsuario, null);
        foreach (var p in personas)
            if (saldos.TryGetValue(p.Id, out var s)) (p.PorCobrar, p.PorPagar) = s;

        return new BaseResponse<List<PersonaModel>> { Success = true, Data = personas };
    }

    public async Task<BaseResponse> Save(int idUsuario, PersonaModel persona)
    {
        var check = Validar(persona);
        if (!check.Success) return check;

        var nombre = persona.Nombre.Trim();
        if (await context.Personas.AnyAsync(a => a.UsuarioId == idUsuario && a.Nombre == nombre))
            return new BaseResponse { Message = "Persona ya existe" };

        var nueva = new Persona { UsuarioId = idUsuario, Nombre = nombre };
        context.Personas.Add(nueva);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = nueva.Id };
    }

    public async Task<BaseResponse> Edit(int idUsuario, PersonaModel persona)
    {
        var check = Validar(persona);
        if (!check.Success) return check;

        var actual = await context.Personas.FirstOrDefaultAsync(a => a.Id == persona.Id && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "Persona no existe" };

        var nombre = persona.Nombre.Trim();
        if (await context.Personas.AnyAsync(a => a.Id != actual.Id && a.UsuarioId == idUsuario && a.Nombre == nombre))
            return new BaseResponse { Message = "Persona ya existe" };

        actual.Nombre = nombre;
        actual.Activa = persona.Activa;

        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = actual.Id };
    }

    // Por cobrar = prestado - abonado por la persona; por pagar = recibido - pagado por mí.
    private async Task<Dictionary<int, (decimal PorCobrar, decimal PorPagar)>> Saldos(int idUsuario, int? idPersona)
    {
        var totales = await context.Operacions.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.PersonaId != null && (idPersona == null || a.PersonaId == idPersona))
            .GroupBy(a => new { a.PersonaId, a.TipoOperacion })
            .Select(g => new { PersonaId = g.Key.PersonaId!.Value, g.Key.TipoOperacion, Total = g.Sum(x => x.Monto) })
            .ToListAsync();

        return totales.GroupBy(t => t.PersonaId).ToDictionary(
            g => g.Key,
            g =>
            {
                decimal Suma(string tipo) => g.Where(t => t.TipoOperacion == tipo).Sum(t => t.Total);
                return (Suma(TipoOperacion.PrestamoOtorgado) - Suma(TipoOperacion.CobroPrestamo),
                        Suma(TipoOperacion.DeudaAdquirida) - Suma(TipoOperacion.PagoDeudaPersona));
            });
    }

    private static PersonaModel ToModel(Persona a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Activa = a.Activa
    };

    private static BaseResponse Validar(PersonaModel p)
    {
        if (string.IsNullOrWhiteSpace(p.Nombre)) return new BaseResponse { Message = "Nombre vacío" };
        if (p.Nombre.Trim().Length > 150) return new BaseResponse { Message = "Nombre supera los caracteres permitidos" };
        return new BaseResponse { Success = true };
    }
}
