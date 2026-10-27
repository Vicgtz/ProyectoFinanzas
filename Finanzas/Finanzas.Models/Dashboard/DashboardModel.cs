namespace Finanzas.Models.Dashboard;

public class DashboardModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }

    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
    public decimal Diferencia => Ingresos - Gastos;

    // Dinero propio (efectivo y débito) separado del crédito, que no se considera dinero propio.
    public decimal DineroPropio { get; set; }
    public decimal LimiteCreditoTotal { get; set; }
    public decimal CreditoDisponible { get; set; }
    public decimal DeudaTarjetas { get; set; }

    public List<GastoCategoriaModel> GastosPorCategoria { get; set; } = [];

    // Pagos de tarjeta pendientes (siempre respecto a hoy, sin importar el mes consultado).
    public List<Finanzas.Models.Cuenta.PagoTarjetaModel> PagosTarjeta { get; set; } = [];

    // Presupuestos del mes consultado (vacío si no hay ninguno).
    public Finanzas.Models.Presupuesto.PresupuestoResumenModel Presupuestos { get; set; } = new();
    public List<MesResumenModel> Meses { get; set; } = [];
    public List<IndicadorModel> Indicadores { get; set; } = [];
}

public class GastoCategoriaModel
{
    public string Nombre { get; set; } = "";
    public decimal Monto { get; set; }
    public decimal Porcentaje { get; set; }
}

// Un mes de la serie histórica (el último elemento es el mes consultado).
public class MesResumenModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
}

public static class NivelIndicador
{
    public const string Bueno = "bueno";
    public const string Regular = "regular";
    public const string Alerta = "alerta";
    public const string SinDatos = "sin-datos";
}

public class IndicadorModel
{
    public string Nombre { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Nivel { get; set; } = NivelIndicador.SinDatos;
}
