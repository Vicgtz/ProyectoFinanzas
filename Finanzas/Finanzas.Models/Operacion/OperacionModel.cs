namespace Finanzas.Models.Operacion;

public static class TipoOperacion
{
    public const string Gasto = "Gasto";
    public const string Ingreso = "Ingreso";
    public const string Transferencia = "Transferencia";
    public const string PagoTarjeta = "PagoTarjeta";
}

// Datos de entrada para crear o editar una operación.
// Gasto: CuentaOrigenId. Ingreso: CuentaDestinoId.
// Transferencia: CuentaOrigenId y CuentaDestinoId (débito/efectivo).
// PagoTarjeta: CuentaOrigenId (débito/efectivo) y CuentaDestinoId (tarjeta de crédito).
public class OperacionRequest
{
    public long Id { get; set; }
    public string TipoOperacion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public int? CuentaOrigenId { get; set; }
    public int? CuentaDestinoId { get; set; }
}

public class OperacionModel
{
    public long Id { get; set; }
    public string TipoOperacion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public List<MovimientoModel> Movimientos { get; set; } = [];
}

// Importe es el cambio aplicado al saldo de la cuenta (en crédito, el saldo es la deuda).
public class MovimientoModel
{
    public int CuentaId { get; set; }
    public string CuentaNombre { get; set; } = "";
    public decimal Importe { get; set; }
}

public class OperacionFiltro
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? TipoOperacion { get; set; }
    public int? CuentaId { get; set; }
    public int? CategoriaId { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}
