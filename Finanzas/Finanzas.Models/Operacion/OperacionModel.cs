namespace Finanzas.Models.Operacion;

public static class TipoOperacion
{
    public const string Gasto = "Gasto";
    public const string Ingreso = "Ingreso";
    public const string Transferencia = "Transferencia";
    public const string PagoTarjeta = "PagoTarjeta";

    // Préstamos y deudas personales (siempre ligados a una persona).
    public const string PrestamoOtorgado = "PrestamoOtorgado";   // yo presto: sale dinero, la persona me debe
    public const string DeudaAdquirida = "DeudaAdquirida";       // me prestan: entra dinero, yo debo
    public const string CobroPrestamo = "CobroPrestamo";         // la persona me abona: entra dinero, me debe menos
    public const string PagoDeudaPersona = "PagoDeudaPersona";   // yo abono: sale dinero, debo menos
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

    // Obligatorio en préstamos y deudas personales.
    public int? PersonaId { get; set; }

    // Solo en PrestamoOtorgado y DeudaAdquirida: si es false, el préstamo se guarda sin mover ninguna cuenta.
    public bool AfectaCuenta { get; set; } = true;

    // Solo para gastos en tarjeta de crédito: compra a meses sin intereses (2 a 60).
    public int? NumeroMeses { get; set; }
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
    public int? PersonaId { get; set; }
    public string? PersonaNombre { get; set; }
    public List<MovimientoModel> Movimientos { get; set; } = [];
    public MsiOperacionModel? Msi { get; set; }
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
    public int? PersonaId { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
}

public class MsiOperacionModel
{
    public int NumeroMeses { get; set; }
    public decimal ImporteMensual { get; set; }
    public DateTime FechaPrimeraMensualidad { get; set; }
}

public static class TipoOperacionTexto
{
    public static string Nombre(string tipo) => tipo switch
    {
        TipoOperacion.PagoTarjeta => "Pago de tarjeta",
        TipoOperacion.PrestamoOtorgado => "Préstamo otorgado",
        TipoOperacion.DeudaAdquirida => "Deuda adquirida",
        TipoOperacion.CobroPrestamo => "Abono recibido",
        TipoOperacion.PagoDeudaPersona => "Pago de deuda",
        _ => tipo
    };

    public static bool EsPersona(string tipo) =>
        tipo is TipoOperacion.PrestamoOtorgado or TipoOperacion.DeudaAdquirida
            or TipoOperacion.CobroPrestamo or TipoOperacion.PagoDeudaPersona;
}
