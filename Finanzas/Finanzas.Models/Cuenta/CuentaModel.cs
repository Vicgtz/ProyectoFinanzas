namespace Finanzas.Models.Cuenta;

public class CuentaModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string TipoCuenta { get; set; } = "";
    public decimal Saldo { get; set; }
    public decimal? LimiteCredito { get; set; }
    public byte? DiaCorte { get; set; }

    // Días después del corte para pagar sin intereses (solo crédito, opcional).
    public byte? DiasParaPago { get; set; }
    public bool Activa { get; set; } = true;
}

public class TipoCuentaItem
{
    public const string Efectivo = "E";
    public const string Debito = "D";
    public const string Credito = "C";

    public string Clave { get; set; } = "";
    public string Descripcion { get; set; } = "";

    public static List<TipoCuentaItem> GetList() =>
    [
        new() { Clave = Efectivo, Descripcion = "Efectivo" },
        new() { Clave = Debito, Descripcion = "Débito" },
        new() { Clave = Credito, Descripcion = "Crédito" }
    ];
}
