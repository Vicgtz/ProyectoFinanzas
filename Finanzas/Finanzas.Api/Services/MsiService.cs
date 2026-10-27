using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Msi;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Las mensualidades se proyectan a partir de CompraMsi; no existen como gastos. La compra ya registró el gasto completo
// y ya redujo el crédito disponible, así que aquí solo se calcula qué se cobrará y cuándo.
public class MsiService(FinanzasDbContext context)
{
    private const int MesesCalendario = 12;

    public async Task<BaseResponse<MsiResumenModel>> GetResumen(int idUsuario)
    {
        var compras = await context.CompraMsis.AsNoTracking()
            .Where(a => a.Cuenta.UsuarioId == idUsuario)
            .Select(a => new
            {
                a.OperacionId,
                Descripcion = a.Operacion.Descripcion,
                Fecha = a.Operacion.Fecha,
                Cuenta = a.Cuenta.Nombre,
                a.Cuenta.DiaCorte,
                a.MontoCompra,
                a.NumeroMeses,
                a.ImporteMensual,
                a.FechaPrimeraMensualidad
            })
            .ToListAsync();

        var hoy = DateTime.Today;
        var resumen = new MsiResumenModel();
        var porMes = new Dictionary<(int, int), decimal>();

        foreach (var c in compras)
        {
            var fechas = Fechas(c.FechaPrimeraMensualidad, c.NumeroMeses, c.DiaCorte);
            var transcurridas = fechas.Count(f => f <= hoy);
            var restantes = c.NumeroMeses - transcurridas;

            var pendientes = 0m;
            for (var i = transcurridas; i < c.NumeroMeses; i++)
            {
                var importe = Importe(c.MontoCompra, c.NumeroMeses, c.ImporteMensual, i);
                pendientes += importe;
                var llave = (fechas[i].Year, fechas[i].Month);
                porMes[llave] = porMes.GetValueOrDefault(llave) + importe;
            }

            resumen.TotalPendiente += pendientes;
            resumen.Planes.Add(new MsiPlanModel
            {
                OperacionId = c.OperacionId,
                Descripcion = string.IsNullOrWhiteSpace(c.Descripcion) ? "Compra a MSI" : c.Descripcion,
                CuentaNombre = c.Cuenta,
                FechaCompra = c.Fecha,
                MontoCompra = c.MontoCompra,
                NumeroMeses = c.NumeroMeses,
                ImporteMensual = c.ImporteMensual,
                MensualidadesTranscurridas = transcurridas,
                MensualidadesRestantes = restantes,
                SaldoPendiente = pendientes,
                ProximaMensualidad = restantes > 0 ? fechas[transcurridas] : null,
                UltimaMensualidad = fechas[^1]
            });
        }

        for (var i = 0; i < MesesCalendario; i++)
        {
            var mes = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(i);
            resumen.Calendario.Add(new MsiMesModel { Anio = mes.Year, Mes = mes.Month, Importe = porMes.GetValueOrDefault((mes.Year, mes.Month)) });
        }
        resumen.MesActual = resumen.Calendario[0].Importe;
        resumen.MesSiguiente = resumen.Calendario[1].Importe;

        // Primero los planes con mensualidades por cobrar, el más próximo arriba; luego los liquidados.
        resumen.Planes = resumen.Planes
            .OrderBy(p => p.MensualidadesRestantes == 0)
            .ThenBy(p => p.ProximaMensualidad)
            .ThenByDescending(p => p.FechaCompra)
            .ToList();

        return new BaseResponse<MsiResumenModel> { Success = true, Data = resumen };
    }

    // Fecha de cada mensualidad: una por corte, a partir de la primera. Con corte 31 cae el último día de los meses cortos.
    public static List<DateTime> Fechas(DateTime primera, int meses, byte? diaCorte)
    {
        var dia = diaCorte ?? primera.Day;
        var lista = new List<DateTime>(meses);
        for (var i = 0; i < meses; i++)
        {
            var mes = new DateTime(primera.Year, primera.Month, 1).AddMonths(i);
            lista.Add(new DateTime(mes.Year, mes.Month, Math.Min(dia, DateTime.DaysInMonth(mes.Year, mes.Month))));
        }
        return lista;
    }

    // La última mensualidad absorbe el centavo de redondeo para que la suma sea exactamente el monto de la compra.
    public static decimal Importe(decimal monto, int meses, decimal mensual, int indice) =>
        indice == meses - 1 ? monto - mensual * (meses - 1) : mensual;
}
