using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Operacion;
using Finanzas.Models.Presupuesto;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Presupuesto = monto fijo mensual por categoría. Lo gastado sale de las operaciones de tipo Gasto del mes
// (igual que el dashboard: las compras a MSI cuentan completas en el mes de la compra).
public class PresupuestoService(FinanzasDbContext context)
{
    public async Task<BaseResponse<PresupuestoResumenModel>> GetResumen(int idUsuario, int? anio, int? mes)
    {
        var hoy = DateTime.Today;
        var anioConsulta = anio ?? hoy.Year;
        var mesConsulta = mes ?? hoy.Month;
        if (anioConsulta is < 2000 or > 2100 || mesConsulta is < 1 or > 12)
            return new BaseResponse<PresupuestoResumenModel> { Message = "Mes inválido" };

        var inicio = new DateTime(anioConsulta, mesConsulta, 1);
        var fin = inicio.AddMonths(1);

        var presupuestos = await context.Presupuestos.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario)
            .Select(a => new { a.Id, a.CategoriaId, a.Monto, Nombre = a.Categoria.Nombre, Emoji = a.Categoria.Emoji })
            .ToListAsync();

        var gastosPorCategoria = await context.Operacions.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.TipoOperacion == TipoOperacion.Gasto && a.Fecha >= inicio && a.Fecha < fin)
            .GroupBy(a => a.CategoriaId)
            .Select(g => new { CategoriaId = g.Key, Total = g.Sum(x => x.Monto) })
            .ToListAsync();

        var gastado = gastosPorCategoria.Where(g => g.CategoriaId != null).ToDictionary(g => g.CategoriaId!.Value, g => g.Total);

        var items = presupuestos
            .Select(p => new PresupuestoItemModel
            {
                Id = p.Id,
                CategoriaId = p.CategoriaId,
                Categoria = string.IsNullOrEmpty(p.Emoji) ? p.Nombre : $"{p.Emoji} {p.Nombre}",
                Monto = p.Monto,
                Gastado = gastado.GetValueOrDefault(p.CategoriaId)
            })
            .OrderByDescending(i => i.Porcentaje).ThenBy(i => i.Categoria)
            .ToList();

        var totalGastos = gastosPorCategoria.Sum(g => g.Total);
        var totalGastado = items.Sum(i => i.Gastado);

        return new BaseResponse<PresupuestoResumenModel>
        {
            Success = true,
            Data = new PresupuestoResumenModel
            {
                Anio = anioConsulta,
                Mes = mesConsulta,
                Items = items,
                TotalPresupuestado = items.Sum(i => i.Monto),
                TotalGastado = totalGastado,
                GastoSinPresupuesto = totalGastos - totalGastado
            }
        };
    }

    public async Task<BaseResponse> Save(int idUsuario, PresupuestoModel modelo)
    {
        var check = await Validar(modelo);
        if (!check.Success) return check;

        if (await context.Presupuestos.AnyAsync(a => a.UsuarioId == idUsuario && a.CategoriaId == modelo.CategoriaId))
            return new BaseResponse { Message = "Esa categoría ya tiene presupuesto; edítalo en lugar de crear otro" };

        var nuevo = new Presupuesto
        {
            UsuarioId = idUsuario,
            CategoriaId = modelo.CategoriaId,
            Monto = decimal.Round(modelo.Monto, 2)
        };

        context.Presupuestos.Add(nuevo);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            return new BaseResponse { Message = "Esa categoría ya tiene presupuesto; edítalo en lugar de crear otro" };
        }
        return new BaseResponse { Success = true, Message = "Guardado", Id = nuevo.Id };
    }

    // Solo cambia el monto: la categoría de un presupuesto no se reasigna (se borra y se crea otro).
    public async Task<BaseResponse> Edit(int idUsuario, PresupuestoModel modelo)
    {
        if (decimal.Round(modelo.Monto, 2) <= 0) return new BaseResponse { Message = "El monto debe ser mayor a cero" };

        var actual = await context.Presupuestos.FirstOrDefaultAsync(a => a.Id == modelo.Id && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "El presupuesto no existe" };

        actual.Monto = decimal.Round(modelo.Monto, 2);
        actual.FechaActualizacion = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Guardado", Id = actual.Id };
    }

    public async Task<BaseResponse> Delete(int idUsuario, int idPresupuesto)
    {
        var actual = await context.Presupuestos.FirstOrDefaultAsync(a => a.Id == idPresupuesto && a.UsuarioId == idUsuario);
        if (actual == null) return new BaseResponse { Message = "El presupuesto no existe" };

        context.Presupuestos.Remove(actual);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Eliminado" };
    }

    private async Task<BaseResponse> Validar(PresupuestoModel m)
    {
        if (decimal.Round(m.Monto, 2) <= 0) return new BaseResponse { Message = "El monto debe ser mayor a cero" };
        if (!await context.Categorias.AnyAsync(a => a.Id == m.CategoriaId && a.Activa))
            return new BaseResponse { Message = "Categoría no encontrada" };
        return new BaseResponse { Success = true };
    }
}
