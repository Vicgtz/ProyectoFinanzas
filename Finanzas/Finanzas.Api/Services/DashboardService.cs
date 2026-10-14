using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Dashboard;
using Finanzas.Models.Operacion;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Solo Ingreso y Gasto cuentan como ingresos/gastos: transferencias y pagos de tarjeta no duplican el gasto original.
public class DashboardService(FinanzasDbContext context)
{
    private const int MesesHistorial = 6;
    private const int MaxCategorias = 8;

    public async Task<BaseResponse<DashboardModel>> Get(int idUsuario, int? anio, int? mes)
    {
        var hoy = DateTime.Today;
        var anioConsulta = anio ?? hoy.Year;
        var mesConsulta = mes ?? hoy.Month;
        if (anioConsulta is < 2000 or > 2100 || mesConsulta is < 1 or > 12)
            return new BaseResponse<DashboardModel> { Message = "Mes inválido" };

        var inicio = new DateTime(anioConsulta, mesConsulta, 1);
        var fin = inicio.AddMonths(1);
        var inicioHistorial = inicio.AddMonths(-(MesesHistorial - 1));

        var cuentas = await context.Cuentas.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.Activa)
            .Select(a => new { a.TipoCuenta, a.Saldo, a.LimiteCredito })
            .ToListAsync();

        var operaciones = await context.Operacions.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.Fecha >= inicioHistorial && a.Fecha < fin
                        && (a.TipoOperacion == TipoOperacion.Ingreso || a.TipoOperacion == TipoOperacion.Gasto))
            .Select(a => new
            {
                a.TipoOperacion,
                a.Monto,
                a.Fecha,
                Categoria = a.Categoria == null ? null : a.Categoria.Nombre,
                Emoji = a.Categoria == null ? null : a.Categoria.Emoji,
                Clasificacion = a.Categoria == null ? null : a.Categoria.ClasificacionGasto
            })
            .ToListAsync();

        var modelo = new DashboardModel { Anio = anioConsulta, Mes = mesConsulta };

        var credito = cuentas.Where(c => c.TipoCuenta == TipoCuentaItem.Credito).ToList();
        modelo.DineroPropio = cuentas.Where(c => c.TipoCuenta != TipoCuentaItem.Credito).Sum(c => c.Saldo);
        modelo.LimiteCreditoTotal = credito.Sum(c => c.LimiteCredito ?? 0);
        modelo.DeudaTarjetas = credito.Sum(c => c.Saldo);
        modelo.CreditoDisponible = modelo.LimiteCreditoTotal - modelo.DeudaTarjetas;

        for (var i = 0; i < MesesHistorial; i++)
        {
            var m = inicioHistorial.AddMonths(i);
            var delMes = operaciones.Where(o => o.Fecha.Year == m.Year && o.Fecha.Month == m.Month).ToList();
            modelo.Meses.Add(new MesResumenModel
            {
                Anio = m.Year,
                Mes = m.Month,
                Ingresos = delMes.Where(o => o.TipoOperacion == TipoOperacion.Ingreso).Sum(o => o.Monto),
                Gastos = delMes.Where(o => o.TipoOperacion == TipoOperacion.Gasto).Sum(o => o.Monto)
            });
        }

        var actual = modelo.Meses[^1];
        modelo.Ingresos = actual.Ingresos;
        modelo.Gastos = actual.Gastos;

        var gastosMes = operaciones.Where(o => o.TipoOperacion == TipoOperacion.Gasto && o.Fecha >= inicio).ToList();
        var grupos = gastosMes
            .GroupBy(o => o.Categoria == null ? "Sin categoría" : (string.IsNullOrEmpty(o.Emoji) ? o.Categoria : $"{o.Emoji} {o.Categoria}"))
            .Select(g => new GastoCategoriaModel { Nombre = g.Key, Monto = g.Sum(o => o.Monto) })
            .OrderByDescending(g => g.Monto)
            .ToList();

        if (grupos.Count > MaxCategorias)
        {
            var resto = grupos.Skip(MaxCategorias - 1).Sum(g => g.Monto);
            grupos = grupos.Take(MaxCategorias - 1).Append(new GastoCategoriaModel { Nombre = "Otras", Monto = resto }).ToList();
        }
        foreach (var g in grupos) g.Porcentaje = modelo.Gastos == 0 ? 0 : decimal.Round(g.Monto / modelo.Gastos * 100, 1);
        modelo.GastosPorCategoria = grupos;

        var noEsencial = gastosMes.Where(o => o.Clasificacion == "N").Sum(o => o.Monto);
        modelo.Indicadores = Indicadores(modelo, noEsencial);

        return new BaseResponse<DashboardModel> { Success = true, Data = modelo };
    }

    private static List<IndicadorModel> Indicadores(DashboardModel m, decimal noEsencial)
    {
        var lista = new List<IndicadorModel>();

        // Tasa de ahorro del mes.
        if (m.Ingresos > 0)
        {
            var tasa = m.Diferencia / m.Ingresos * 100;
            lista.Add(new IndicadorModel
            {
                Nombre = "Ahorro del mes",
                Valor = $"{tasa:0.#}%",
                Detalle = tasa >= 0 ? "De tus ingresos que no gastaste" : "Gastaste más de lo que ingresó",
                Nivel = tasa >= 20 ? NivelIndicador.Bueno : tasa >= 0 ? NivelIndicador.Regular : NivelIndicador.Alerta
            });
        }
        else
        {
            lista.Add(new IndicadorModel
            {
                Nombre = "Ahorro del mes",
                Valor = "—",
                Detalle = m.Gastos > 0 ? "Hay gastos pero no ingresos este mes" : "Sin ingresos registrados este mes",
                Nivel = m.Gastos > 0 ? NivelIndicador.Alerta : NivelIndicador.SinDatos
            });
        }

        // Uso del crédito: por debajo de 30% se considera sano.
        if (m.LimiteCreditoTotal > 0)
        {
            var uso = m.DeudaTarjetas / m.LimiteCreditoTotal * 100;
            lista.Add(new IndicadorModel
            {
                Nombre = "Uso del crédito",
                Valor = $"{uso:0.#}%",
                Detalle = "Deuda sobre el límite total de tus tarjetas",
                Nivel = uso <= 30 ? NivelIndicador.Bueno : uso <= 60 ? NivelIndicador.Regular : NivelIndicador.Alerta
            });
        }

        // Gasto no esencial sobre el total gastado.
        if (m.Gastos > 0)
        {
            var pct = noEsencial / m.Gastos * 100;
            lista.Add(new IndicadorModel
            {
                Nombre = "Gasto no esencial",
                Valor = $"{pct:0.#}%",
                Detalle = "Del total gastado en el mes",
                Nivel = pct <= 30 ? NivelIndicador.Bueno : pct <= 50 ? NivelIndicador.Regular : NivelIndicador.Alerta
            });
        }

        // Cuántos meses cubre el dinero propio con el gasto promedio de los meses con gasto.
        var conGasto = m.Meses.Where(x => x.Gastos > 0).ToList();
        if (conGasto.Count > 0)
        {
            var meses = m.DineroPropio / conGasto.Average(x => x.Gastos);
            lista.Add(new IndicadorModel
            {
                Nombre = "Colchón financiero",
                Valor = $"{meses:0.#} meses",
                Detalle = "Tu dinero propio cubre este tiempo de gasto promedio",
                Nivel = meses >= 3 ? NivelIndicador.Bueno : meses >= 1 ? NivelIndicador.Regular : NivelIndicador.Alerta
            });
        }

        return lista;
    }
}
