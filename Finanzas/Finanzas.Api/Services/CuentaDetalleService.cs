using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Corte de tarjeta: las compras hechas el mismo día de corte pertenecen al periodo siguiente,
// por eso un periodo abarca [corte anterior, corte siguiente) y la deuda al corte excluye lo fechado desde ese día.
public class CuentaDetalleService(FinanzasDbContext context)
{
    public async Task<BaseResponse<CuentaDetalleModel>> Get(int idUsuario, int idCuenta)
    {
        var cuenta = await context.Cuentas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == idCuenta && a.UsuarioId == idUsuario);
        if (cuenta == null) return new BaseResponse<CuentaDetalleModel> { Message = "Cuenta no encontrada" };

        var detalle = new CuentaDetalleModel
        {
            Cuenta = new CuentaModel
            {
                Id = cuenta.Id,
                Nombre = cuenta.Nombre,
                TipoCuenta = cuenta.TipoCuenta,
                Saldo = cuenta.Saldo,
                LimiteCredito = cuenta.LimiteCredito,
                DiaCorte = cuenta.DiaCorte,
                Activa = cuenta.Activa
            }
        };

        if (cuenta.TipoCuenta == TipoCuentaItem.Credito && cuenta.DiaCorte != null)
        {
            var hoy = DateTime.Today;
            var ultimo = UltimoCorte(cuenta.DiaCorte.Value, hoy);
            var proximo = FechaCorte(ultimo.AddMonths(1).Year, ultimo.AddMonths(1).Month, cuenta.DiaCorte.Value);

            detalle.Disponible = (cuenta.LimiteCredito ?? 0) - cuenta.Saldo;
            detalle.UltimoCorte = ultimo;
            detalle.ProximoCorte = proximo;
            detalle.DiasParaCorte = (proximo - hoy).Days;
            detalle.ConsumoPeriodoActual = await ImporteDesde(cuenta.Id, ultimo);

            detalle.Cortes = await context.CorteTarjetas.AsNoTracking()
                .Where(a => a.CuentaId == cuenta.Id)
                .OrderByDescending(a => a.FechaCorte)
                .Take(24)
                .Select(a => new CorteModel
                {
                    Id = a.Id,
                    FechaCorte = a.FechaCorte,
                    DeudaAlCorte = a.DeudaAlCorte,
                    CreditoDisponibleAlCorte = a.CreditoDisponibleAlCorte
                })
                .ToListAsync();

            detalle.CortePendiente = detalle.Cortes.All(c => c.FechaCorte.Date != ultimo);
        }

        return new BaseResponse<CuentaDetalleModel> { Success = true, Data = detalle };
    }

    public async Task<BaseResponse> RegistrarCorte(int idUsuario, CorteRequest request)
    {
        var cuenta = await context.Cuentas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.CuentaId && a.UsuarioId == idUsuario);
        if (cuenta == null) return new BaseResponse { Message = "Cuenta no existe" };
        if (cuenta.TipoCuenta != TipoCuentaItem.Credito || cuenta.DiaCorte == null)
            return new BaseResponse { Message = "Solo las tarjetas de crédito tienen corte" };

        var hoy = DateTime.Today;
        var fecha = (request.FechaCorte ?? UltimoCorte(cuenta.DiaCorte.Value, hoy)).Date;
        if (fecha > hoy) return new BaseResponse { Message = "El corte no puede tener fecha futura" };

        if (await context.CorteTarjetas.AnyAsync(a => a.CuentaId == cuenta.Id && a.FechaCorte == fecha))
            return new BaseResponse { Message = "Ya existe un corte registrado en esa fecha" };

        // Lo fechado desde el día de corte pertenece al periodo siguiente, así que se resta de la deuda actual.
        var deuda = Math.Max(0, cuenta.Saldo - await ImporteDesde(cuenta.Id, fecha));
        var corte = new CorteTarjeta
        {
            CuentaId = cuenta.Id,
            FechaCorte = fecha,
            DeudaAlCorte = deuda,
            CreditoDisponibleAlCorte = (cuenta.LimiteCredito ?? 0) - deuda,
            FechaRegistro = DateTime.UtcNow
        };

        context.CorteTarjetas.Add(corte);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Corte registrado", Id = corte.Id };
    }

    public async Task<BaseResponse> EliminarCorte(int idUsuario, long idCorte)
    {
        var corte = await context.CorteTarjetas
            .FirstOrDefaultAsync(a => a.Id == idCorte && a.Cuenta.UsuarioId == idUsuario);
        if (corte == null) return new BaseResponse { Message = "Corte no existe" };

        context.CorteTarjetas.Remove(corte);
        await context.SaveChangesAsync();
        return new BaseResponse { Success = true, Message = "Corte eliminado" };
    }

    // Suma de los movimientos de la cuenta fechados desde `desde` (incluido el día) hasta hoy.
    private async Task<decimal> ImporteDesde(int idCuenta, DateTime desde) =>
        await context.MovimientoCuentas.AsNoTracking()
            .Where(m => m.CuentaId == idCuenta && m.Operacion.Fecha >= desde)
            .SumAsync(m => (decimal?)m.Importe) ?? 0;

    // Fecha de corte más reciente que ya ocurrió (hoy cuenta como ocurrido).
    private static DateTime UltimoCorte(int diaCorte, DateTime hoy)
    {
        var esteMes = FechaCorte(hoy.Year, hoy.Month, diaCorte);
        if (esteMes <= hoy) return esteMes;
        var anterior = hoy.AddMonths(-1);
        return FechaCorte(anterior.Year, anterior.Month, diaCorte);
    }

    // Un día de corte 31 cae el último día de los meses más cortos.
    private static DateTime FechaCorte(int anio, int mes, int diaCorte) =>
        new(anio, mes, Math.Min(diaCorte, DateTime.DaysInMonth(anio, mes)));
}
