using Finanzas.Data;
using Finanzas.Models;
using Finanzas.Models.Analisis;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Services;

// Solo Ingreso y Gasto cuentan: transferencias, pagos de tarjeta y préstamos no son ingresos ni gastos.
public class AnalisisService(FinanzasDbContext context, MsiService msiService, PresupuestoService presupuestoService)
{
    private const int MesesSerie = 12;
    private const int MesesDeficit = 6;
    private const int MesesBase = 3;
    private const int MaxMesesCompra = 60;

    private sealed record Linea(string Tipo, decimal Monto, DateTime Fecha, string? Categoria, string? Clasificacion, bool EsMsi);

    public async Task<BaseResponse<AnalisisModel>> Get(int idUsuario)
    {
        var hoy = DateTime.Today;
        var inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
        var desde = inicioMesActual.AddMonths(-(MesesSerie - 1));

        var lineas = await Lineas(idUsuario, desde);
        var modelo = new AnalisisModel();

        for (var i = 0; i < MesesSerie; i++)
        {
            var mes = desde.AddMonths(i);
            var delMes = lineas.Where(l => l.Fecha.Year == mes.Year && l.Fecha.Month == mes.Month).ToList();
            var gastos = delMes.Where(l => l.Tipo == TipoOperacion.Gasto).ToList();
            var item = new AnalisisMesModel
            {
                Anio = mes.Year,
                Mes = mes.Month,
                Ingresos = delMes.Where(l => l.Tipo == TipoOperacion.Ingreso).Sum(l => l.Monto),
                Gastos = gastos.Sum(l => l.Monto),
                Esencial = gastos.Where(l => l.Clasificacion == "E").Sum(l => l.Monto),
                NoEsencial = gastos.Where(l => l.Clasificacion == "N").Sum(l => l.Monto)
            };
            item.Otros = item.Gastos - item.Esencial - item.NoEsencial;

            var anterior = modelo.Meses.LastOrDefault();
            if (anterior is { Gastos: > 0 }) item.VariacionGastos = (item.Gastos - anterior.Gastos) / anterior.Gastos * 100;
            modelo.Meses.Add(item);
        }

        modelo.MesesEnDeficit = modelo.Meses.TakeLast(MesesDeficit).Count(m => m.GastaMasQueIngresa);
        modelo.CategoriasEnAumento = CategoriasEnAumento(lineas, inicioMesActual);
        modelo.Alertas = Alertas(modelo, hoy);

        // Los presupuestos rebasados o a punto de rebasarse van primero; si hay alguno, el mensaje de "todo en orden" sobra.
        var alertasPresupuesto = await AlertasPresupuesto(idUsuario, hoy);
        if (alertasPresupuesto.Count > 0)
        {
            modelo.Alertas.RemoveAll(a => a.Nivel is NivelAlerta.Bien or NivelAlerta.Info);
            modelo.Alertas.InsertRange(0, alertasPresupuesto);
        }

        return new BaseResponse<AnalisisModel> { Success = true, Data = modelo };
    }

    // Qué pasaría si haces una compra grande: al contado (meses = 1) o a meses sin intereses.
    public async Task<BaseResponse<ImpactoCompraModel>> GetImpacto(int idUsuario, decimal monto, int meses)
    {
        if (monto <= 0) return new BaseResponse<ImpactoCompraModel> { Message = "El monto debe ser mayor a cero" };
        if (meses is < 1 or > MaxMesesCompra) return new BaseResponse<ImpactoCompraModel> { Message = "Los meses deben estar entre 1 y 60" };

        var hoy = DateTime.Today;
        var inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
        var lineas = await Lineas(idUsuario, inicioMesActual.AddMonths(-MesesBase));

        // Promedio de los meses cerrados (el mes en curso está incompleto) que tuvieron movimientos.
        var cerrados = lineas.Where(l => l.Fecha < inicioMesActual).ToList();
        var mesesConDatos = cerrados.Select(l => (l.Fecha.Year, l.Fecha.Month)).Distinct().Count();
        var ingresoPromedio = mesesConDatos == 0 ? 0 : cerrados.Where(l => l.Tipo == TipoOperacion.Ingreso).Sum(l => l.Monto) / mesesConDatos;
        // Las compras a MSI no entran aquí: su costo mensual se suma aparte como mensualidad.
        var gastoPromedio = mesesConDatos == 0 ? 0 : cerrados.Where(l => l.Tipo == TipoOperacion.Gasto && !l.EsMsi).Sum(l => l.Monto) / mesesConDatos;

        var cuentas = await context.Cuentas.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.Activa)
            .Select(a => new { a.TipoCuenta, a.Saldo, a.LimiteCredito })
            .ToListAsync();
        var dineroPropio = cuentas.Where(c => c.TipoCuenta != TipoCuentaItem.Credito).Sum(c => c.Saldo);
        var disponibleCredito = cuentas.Where(c => c.TipoCuenta == TipoCuentaItem.Credito).Sum(c => (c.LimiteCredito ?? 0) - c.Saldo);

        var msi = (await msiService.GetResumen(idUsuario)).Data;
        var mensualidad = decimal.Round(monto / meses, 2);

        var modelo = new ImpactoCompraModel
        {
            Monto = monto,
            Meses = meses,
            Mensualidad = mensualidad,
            IngresoPromedio = ingresoPromedio,
            GastoPromedio = gastoPromedio,
            DineroPropioActual = dineroPropio,
            LimiteCreditoDisponible = disponibleCredito
        };

        if (meses == 1)
        {
            modelo.DineroPropioDespues = dineroPropio - monto;
            modelo.CabeEnCredito = true;
            modelo.ColchonAntes = Colchon(dineroPropio, gastoPromedio);
            modelo.ColchonDespues = Colchon(modelo.DineroPropioDespues, gastoPromedio);
        }
        else
        {
            // A MSI el dinero propio no baja hoy: el costo llega mes a mes y ocupa crédito desde el primer día.
            modelo.DineroPropioDespues = dineroPropio;
            modelo.CabeEnCredito = monto <= disponibleCredito;
            modelo.ColchonAntes = Colchon(dineroPropio, gastoPromedio);
            modelo.ColchonDespues = modelo.ColchonAntes;
        }

        // Proyección de los próximos meses (hasta 12): lo que queda libre después de gastos normales y mensualidades MSI.
        // La primera mensualidad se asume en el mes siguiente (el corte de la tarjeta puede quedar antes o después de hoy).
        var horizonte = meses == 1 ? 3 : Math.Min(meses + 1, 13);
        for (var i = 0; i < horizonte; i++)
        {
            var mes = inicioMesActual.AddMonths(i);
            var existentes = msi?.Calendario.FirstOrDefault(c => c.Anio == mes.Year && c.Mes == mes.Month)?.Importe ?? 0;
            var nueva = meses > 1 && i >= 1 && i <= meses ? mensualidad : 0;
            modelo.Proyeccion.Add(new ImpactoMesModel
            {
                Anio = mes.Year,
                Mes = mes.Month,
                MsiExistentes = existentes,
                NuevaCompra = nueva,
                FlujoLibre = ingresoPromedio - gastoPromedio - existentes - nueva
            });
        }

        Veredicto(modelo);
        return new BaseResponse<ImpactoCompraModel> { Success = true, Data = modelo };
    }

    // ---- Apoyo ----

    private async Task<List<Linea>> Lineas(int idUsuario, DateTime desde) =>
        await context.Operacions.AsNoTracking()
            .Where(a => a.UsuarioId == idUsuario && a.Fecha >= desde
                        && (a.TipoOperacion == TipoOperacion.Ingreso || a.TipoOperacion == TipoOperacion.Gasto))
            .Select(a => new Linea(
                a.TipoOperacion,
                a.Monto,
                a.Fecha,
                a.Categoria == null ? null : a.Categoria.Nombre,
                a.Categoria == null ? null : a.Categoria.ClasificacionGasto,
                a.CompraMsi != null))
            .ToListAsync();

    private static decimal? Colchon(decimal dinero, decimal gastoMensual) =>
        gastoMensual <= 0 ? null : decimal.Round(Math.Max(dinero, 0) / gastoMensual, 1);

    // Categorías cuyo gasto del mes en curso ya supera al del mes anterior, de mayor a menor aumento.
    private static List<CategoriaCambioModel> CategoriasEnAumento(List<Linea> lineas, DateTime inicioMesActual)
    {
        var anterior = inicioMesActual.AddMonths(-1);
        var gastos = lineas.Where(l => l.Tipo == TipoOperacion.Gasto).ToList();

        Dictionary<string, decimal> PorCategoria(Func<Linea, bool> filtro) =>
            gastos.Where(filtro).GroupBy(l => l.Categoria ?? "Sin categoría").ToDictionary(g => g.Key, g => g.Sum(l => l.Monto));

        var actuales = PorCategoria(l => l.Fecha >= inicioMesActual);
        var previos = PorCategoria(l => l.Fecha >= anterior && l.Fecha < inicioMesActual);

        return actuales
            .Select(c => new CategoriaCambioModel
            {
                Nombre = c.Key,
                Clasificacion = gastos.FirstOrDefault(l => (l.Categoria ?? "Sin categoría") == c.Key)?.Clasificacion ?? "X",
                Actual = c.Value,
                Anterior = previos.GetValueOrDefault(c.Key)
            })
            .Where(c => c.Aumento > 0)
            .OrderByDescending(c => c.Aumento)
            .Take(5)
            .ToList();
    }

    private async Task<List<AlertaModel>> AlertasPresupuesto(int idUsuario, DateTime hoy)
    {
        var resumen = (await presupuestoService.GetResumen(idUsuario, hoy.Year, hoy.Month)).Data;
        var alertas = new List<AlertaModel>();
        if (resumen == null) return alertas;

        foreach (var p in resumen.Items.Where(i => i.Nivel != Finanzas.Models.Dashboard.NivelIndicador.Bueno))
        {
            alertas.Add(p.Porcentaje >= 100
                ? new AlertaModel { Nivel = NivelAlerta.Alerta, Titulo = $"Rebasaste tu presupuesto de {p.Categoria}", Detalle = $"Llevas {Formato(p.Gastado)} de {Formato(p.Monto)} ({decimal.Round(p.Porcentaje)}%): te pasaste {Formato(-p.Disponible)}." }
                : new AlertaModel { Nivel = NivelAlerta.Aviso, Titulo = $"Tu presupuesto de {p.Categoria} va al {decimal.Round(p.Porcentaje)}%", Detalle = $"Llevas {Formato(p.Gastado)} de {Formato(p.Monto)}; te quedan {Formato(p.Disponible)} este mes." });
        }

        // Primero las alertas, luego los avisos.
        return alertas.OrderBy(a => a.Nivel == NivelAlerta.Alerta ? 0 : 1).ToList();
    }

    private static List<AlertaModel> Alertas(AnalisisModel modelo, DateTime hoy)
    {
        var alertas = new List<AlertaModel>();
        var actual = modelo.Meses[^1];
        var previos = modelo.Meses.Skip(modelo.Meses.Count - 1 - MesesBase).Take(MesesBase).Where(m => m.Gastos > 0 || m.Ingresos > 0).ToList();

        // Déficit: gastar más de lo que se ingresa.
        if (actual.GastaMasQueIngresa)
            alertas.Add(new AlertaModel { Nivel = NivelAlerta.Alerta, Titulo = "Este mes gastas más de lo que ingresas", Detalle = $"Llevas {Formato(actual.Gastos)} de gasto contra {Formato(actual.Ingresos)} de ingreso." });
        if (modelo.MesesEnDeficit >= 2)
            alertas.Add(new AlertaModel { Nivel = NivelAlerta.Aviso, Titulo = $"{modelo.MesesEnDeficit} de los últimos {MesesDeficit} meses con déficit", Detalle = "Revisa en qué categorías puedes recortar para equilibrar tus ingresos." });

        if (previos.Count > 0)
        {
            var gastoBase = previos.Average(m => m.Gastos);
            if (gastoBase > 0 && actual.Gastos > gastoBase * 1.2m)
                alertas.Add(new AlertaModel { Nivel = NivelAlerta.Aviso, Titulo = "Tu gasto del mes ya supera tu promedio", Detalle = $"Llevas {Formato(actual.Gastos)}, {PorcentajeSobre(actual.Gastos, gastoBase)}% más que el promedio de los {previos.Count} meses anteriores ({Formato(gastoBase)})." });

            // Incremento de gasto no esencial contra el promedio de los meses anteriores.
            var noEsencialBase = previos.Average(m => m.NoEsencial);
            if (noEsencialBase > 0 && actual.NoEsencial > noEsencialBase * 1.2m)
                alertas.Add(new AlertaModel { Nivel = NivelAlerta.Aviso, Titulo = "Subió tu gasto no esencial", Detalle = $"Llevas {Formato(actual.NoEsencial)} en gastos no esenciales, {PorcentajeSobre(actual.NoEsencial, noEsencialBase)}% más que tu promedio ({Formato(noEsencialBase)})." });
        }

        if (actual.Gastos > 0 && actual.NoEsencial / actual.Gastos > 0.5m)
            alertas.Add(new AlertaModel { Nivel = NivelAlerta.Aviso, Titulo = "Más de la mitad de tu gasto es no esencial", Detalle = $"{decimal.Round(actual.NoEsencial / actual.Gastos * 100)}% de lo que gastas este mes." });

        if (alertas.Count == 0)
        {
            alertas.Add(actual.Ingresos == 0 && actual.Gastos == 0
                ? new AlertaModel { Nivel = NivelAlerta.Info, Titulo = "Aún no hay movimientos este mes", Detalle = "Registra ingresos y gastos para ver tu análisis." }
                : new AlertaModel { Nivel = NivelAlerta.Bien, Titulo = "Tus finanzas del mes van en orden", Detalle = "No hay señales de alerta: gastas dentro de lo que ingresas y tu gasto va en línea con tu promedio." });
        }
        return alertas;
    }

    private static decimal PorcentajeSobre(decimal valor, decimal baseValor) => decimal.Round((valor - baseValor) / baseValor * 100);

    private static string Formato(decimal valor) => valor.ToString("C2", new System.Globalization.CultureInfo("es-MX"));

    // Veredicto: al contado importa cuánto colchón queda; a MSI, que el flujo mensual siga siendo positivo.
    private static void Veredicto(ImpactoCompraModel m)
    {
        if (m.Meses == 1)
        {
            if (m.DineroPropioDespues < 0)
            {
                m.Veredicto = NivelAlerta.Alerta;
                m.Resumen = $"No te alcanza al contado: te faltarían {Formato(-m.DineroPropioDespues)}.";
            }
            else if (m.ColchonDespues is null)
            {
                m.Veredicto = NivelAlerta.Aviso;
                m.Resumen = "Te alcanza, pero no hay suficientes gastos registrados para medir tu colchón.";
            }
            else if (m.ColchonDespues >= 3)
            {
                m.Veredicto = NivelAlerta.Bien;
                m.Resumen = $"Es viable: después de pagarla te quedan {m.ColchonDespues:0.#} meses de gasto cubiertos.";
            }
            else
            {
                m.Veredicto = m.ColchonDespues >= 1 ? NivelAlerta.Aviso : NivelAlerta.Alerta;
                m.Resumen = $"Te alcanza, pero tu colchón bajaría de {m.ColchonAntes:0.#} a {m.ColchonDespues:0.#} meses de gasto.";
            }
            return;
        }

        var peor = m.Proyeccion.Min(p => p.FlujoLibre);
        var cuota = m.IngresoPromedio > 0 ? m.Mensualidad / m.IngresoPromedio * 100 : 0;
        if (!m.CabeEnCredito)
        {
            m.Veredicto = NivelAlerta.Alerta;
            m.Resumen = $"No cabe en tu crédito disponible ({Formato(m.LimiteCreditoDisponible)}).";
        }
        else if (peor < 0)
        {
            m.Veredicto = NivelAlerta.Alerta;
            m.Resumen = $"Con esta mensualidad ({Formato(m.Mensualidad)}) tus gastos y compromisos superarían tu ingreso promedio en algunos meses.";
        }
        else if (m.IngresoPromedio == 0)
        {
            m.Veredicto = NivelAlerta.Aviso;
            m.Resumen = "No hay ingresos registrados en meses anteriores para evaluar si puedes cubrir la mensualidad.";
        }
        else if (cuota > 15 || peor < m.IngresoPromedio * 0.1m)
        {
            m.Veredicto = NivelAlerta.Aviso;
            m.Resumen = $"Es posible pero ajustado: la mensualidad es {cuota:0.#}% de tu ingreso y el mes más apretado te deja {Formato(peor)} libres.";
        }
        else
        {
            m.Veredicto = NivelAlerta.Bien;
            m.Resumen = $"Es viable: la mensualidad es {cuota:0.#}% de tu ingreso y siempre te quedan al menos {Formato(peor)} libres al mes.";
        }
    }
}
