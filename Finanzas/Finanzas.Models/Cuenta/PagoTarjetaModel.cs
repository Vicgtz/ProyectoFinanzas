namespace Finanzas.Models.Cuenta;

// Pago pendiente de una tarjeta para no generar intereses: lo que se debía al último corte,
// menos los pagos hechos desde entonces.
public class PagoTarjetaModel
{
    public int CuentaId { get; set; }
    public string Nombre { get; set; } = "";
    public DateTime Corte { get; set; }
    public DateTime FechaLimite { get; set; }

    // Negativo si ya venció.
    public int DiasRestantes { get; set; }
    // Deuda total de la tarjeta al corte (incluye las compras a MSI completas).
    public decimal DeudaAlCorte { get; set; }

    // Parte de esa deuda que son mensualidades de MSI que todavía no se facturan.
    public decimal MsiPorFacturar { get; set; }

    // Lo que el estado de cuenta pide pagar para no generar intereses.
    public decimal PagoSinIntereses => Math.Max(0, DeudaAlCorte - MsiPorFacturar);
    public decimal Pagado { get; set; }
    public decimal Pendiente { get; set; }
    public bool Vencido => DiasRestantes < 0;
}
