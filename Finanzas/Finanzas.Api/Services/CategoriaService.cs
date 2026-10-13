using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Categoria;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Las categorías son un catálogo global (no pertenecen a un usuario), por eso solo se consulta.
public class CategoriaService(FinanzasDbContext context)
{
    public async Task<BaseResponse<CategoriaModel>> Get(int idCategoria)
    {
        var categoria = await context.Categorias.AsNoTracking()
            .Where(a => a.Id == idCategoria)
            .Select(a => ToModel(a))
            .FirstOrDefaultAsync();

        if (categoria == null) return new BaseResponse<CategoriaModel> { Message = "Categoría no encontrada" };
        return new BaseResponse<CategoriaModel> { Success = true, Data = categoria };
    }

    public async Task<BaseResponse<List<CategoriaModel>>> GetList(string? tipoSugerido = null)
    {
        var query = context.Categorias.AsNoTracking().Where(a => a.Activa);
        if (!string.IsNullOrEmpty(tipoSugerido))
            query = query.Where(a => a.TipoSugerido == tipoSugerido);

        var categorias = await query
            .OrderBy(a => a.Orden).ThenBy(a => a.Nombre)
            .Select(a => ToModel(a))
            .ToListAsync();

        return new BaseResponse<List<CategoriaModel>> { Success = true, Data = categorias };
    }

    private static CategoriaModel ToModel(Categoria a) => new()
    {
        Id = a.Id,
        Nombre = a.Nombre,
        Emoji = a.Emoji,
        TipoSugerido = a.TipoSugerido,
        ClasificacionGasto = a.ClasificacionGasto,
        Activa = a.Activa,
        Orden = a.Orden
    };
}
