namespace Finanzas.Models.Cuenta;

public class CuentaDetalleModel
{
    public CuentaModel Cuenta { get; set; } = new();

    // Solo tarjetas de crédito.
    public decimal? Disponible { get; set; }
    public DateTime? UltimoCorte { get; set; }
    public DateTime? ProximoCorte { get; set; }
    public int? DiasParaCorte { get; set; }

    // Lo cargado a la tarjeta desde el último corte (la compra del día de corte cuenta en el periodo nuevo).
    public decimal ConsumoPeriodoActual { get; set; }

    // El último corte ya ocurrió pero todavía no se ha registrado en el historial.
    public bool CortePendiente { get; set; }

    public List<CorteModel> Cortes { get; set; } = [];
}

public class CorteModel
{
    public long Id { get; set; }
    public DateTime FechaCorte { get; set; }
    public decimal DeudaAlCorte { get; set; }
    public decimal CreditoDisponibleAlCorte { get; set; }
}

public class CorteRequest
{
    public int CuentaId { get; set; }

    // Si no se indica, se usa el último corte que ya ocurrió según el día de corte de la tarjeta.
    public DateTime? FechaCorte { get; set; }
}
