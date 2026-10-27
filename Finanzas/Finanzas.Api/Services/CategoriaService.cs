using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Categoria;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Hay categorías generales (UsuarioId NULL, solo lectura, de todos) y categorías propias
// (UsuarioId = usuario): solo las ve y las modifica su dueño.
public class CategoriaService(FinanzasDbContext context)
{
    public async Task<BaseResponse<CategoriaModel>> Get(int idUsuario, int idCategoria)
    {
        var categoria = await context.Categorias.AsNoTracking()
            .Where(a => a.Id == idCategoria && (a.UsuarioId == null || a.UsuarioId == idUsuario))
            .Select(a => ToModel(a, idUsuario))
            .FirstOrDefaultAsync();

        if (categoria == null) return new BaseResponse<CategoriaModel> { Message = "Categoría no encontrada" };
        return new BaseResponse<CategoriaModel> { Success = true, Data = categoria };
    }

    public async Task<BaseResponse<List<CategoriaModel>>> GetList(int idUsuario, string? tipoSugerido = null, bool incluirInactivas = false)
    {
        var query = context.Categorias.AsNoTracking()
            .Where(a => a.UsuarioId == null || a.UsuarioId == idUsuario);
        // Las inactivas solo se muestran (y solo las propias) cuando se administran las categorías.
        query = incluirInactivas ? query.Where(a => a.Activa || a.UsuarioId == idUsuario) : query.Where(a => a.Activa);
        if (!string.IsNullOrEmpty(tipoSugerido))
            query = query.Where(a => a.TipoSugerido == tipoSugerido);

        var categorias = await query
            .OrderBy(a => a.Orden).ThenBy(a => a.Nombre)
            .Select(a => ToModel(a, idUsuario))
            .ToListAsync();

        return new BaseResponse<List<CategoriaModel>> { Success = true, Data = categorias };
    }

    public async Task<BaseResponse> Save(int idUsuario, CategoriaModel modelo)
    {
        var check = await Validar(idUsuario, modelo, null);
        if (!check.Success) return check;

        context.Categorias.Add(new Categoria
        {
            UsuarioId = idUsuario,
            Nombre = modelo.Nombre.Trim(),
            Emoji = string.IsNullOrWhiteSpace(modelo.Emoji) ? null : modelo.Emoji.Trim(),
            TipoSugerido = modelo.TipoSugerido,
            ClasificacionGasto = modelo.TipoSugerido == "G" ? modelo.ClasificacionGasto : "X",
            Activa = true,
            // Después de las generales
            Orden = 1000
        });

        return await Guardar();
    }

    public async Task<BaseResponse> Edit(int idUsuario, CategoriaModel modelo)
    {
        var categoria = await context.Categorias.FirstOrDefaultAsync(a => a.Id == modelo.Id && (a.UsuarioId == null || a.UsuarioId == idUsuario));
        if (categoria == null) return new BaseResponse { Message = "Categoría no encontrada" };
        if (categoria.UsuarioId == null) return new BaseResponse { Message = "No puedes modificar una categoría general" };

        var check = await Validar(idUsuario, modelo, categoria.Id);
        if (!check.Success) return check;

        categoria.Nombre = modelo.Nombre.Trim();
        categoria.Emoji = string.IsNullOrWhiteSpace(modelo.Emoji) ? null : modelo.Emoji.Trim();
        categoria.TipoSugerido = modelo.TipoSugerido;
        categoria.ClasificacionGasto = modelo.TipoSugerido == "G" ? modelo.ClasificacionGasto : "X";
        categoria.Activa = modelo.Activa;

        return await Guardar();
    }

    private async Task<BaseResponse> Validar(int idUsuario, CategoriaModel m, int? idActual)
    {
        var nombre = (m.Nombre ?? "").Trim();
        if (nombre.Length == 0) return new BaseResponse { Message = "Ingresa un nombre" };
        if (nombre.Length > 100) return new BaseResponse { Message = "Nombre supera los caracteres permitidos" };
        if (m.Emoji != null && m.Emoji.Trim().Length > 20) return new BaseResponse { Message = "Emoji supera los caracteres permitidos" };
        if (m.TipoSugerido is not ("I" or "G")) return new BaseResponse { Message = "Tipo de categoría inválido" };
        if (m.TipoSugerido == "G" && m.ClasificacionGasto is not ("E" or "N" or "X"))
            return new BaseResponse { Message = "Clasificación inválida" };

        // No puede repetir una general ni otra propia (sin importar el tipo, para no confundir al elegir).
        var duplicada = await context.Categorias.AnyAsync(a =>
            a.Id != idActual &&
            (a.UsuarioId == null || a.UsuarioId == idUsuario) &&
            a.Nombre == nombre);
        if (duplicada) return new BaseResponse { Message = "Ya existe una categoría con ese nombre" };

        return new BaseResponse { Success = true };
    }

    private async Task<BaseResponse> Guardar()
    {
        try
        {
            await context.SaveChangesAsync();
            return new BaseResponse { Success = true };
        }
        catch (DbUpdateException)
        {
            // Carrera contra el índice único (Usuario, Nombre, Tipo)
            context.ChangeTracker.Clear();
            return new BaseResponse { Message = "Ya existe una categoría con ese nombre" };
        }
    }

    private static CategoriaModel ToModel(Categoria a, int idUsuario) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Emoji = a.Emoji,
        TipoSugerido = a.TipoSugerido,
        ClasificacionGasto = a.ClasificacionGasto,
        Activa = a.Activa,
        Orden = a.Orden,
        EsPropia = a.UsuarioId == idUsuario
    };
}
