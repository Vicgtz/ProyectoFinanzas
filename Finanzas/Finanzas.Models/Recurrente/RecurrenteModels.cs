namespace Finanzas.Models.Recurrente;

// Plantilla de un gasto o ingreso que se repite (renta, suscripciones, nómina).
// No crea movimientos sola: al llegar la fecha aparece como pendiente y se confirma u omite.
public class RecurrenteModel
{
    public int Id { get; set; }
    public string TipoOperacion { get; set; } = "Gasto";
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }

    // Gasto: cuenta de la que sale. Ingreso: cuenta a la que entra.
    public int CuentaId { get; set; }
    public string? CuentaNombre { get; set; }

    public string Frecuencia { get; set; } = FrecuenciaRecurrente.Mensual;

    // Fecha de la primera ocurrencia; también fija el día del mes de las frecuencias mensual y anual.
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    // Siguiente ocurrencia por confirmar. Al crear se toma igual a FechaInicio.
    public DateTime ProximaFecha { get; set; } = DateTime.Today;
    public DateTime? FechaFin { get; set; }
    public bool Activa { get; set; } = true;
}

public class RecurrentePendienteModel
{
    public int RecurrenteId { get; set; }
    public string TipoOperacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public string? CategoriaNombre { get; set; }
    public string CuentaNombre { get; set; } = "";
    public DateTime Fecha { get; set; }

    // Ocurrencias posteriores que también ya vencieron (se atienden una por una).
    public int Atrasadas { get; set; }
}

public class ConfirmarRecurrenteRequest
{
    public int RecurrenteId { get; set; }

    // Fecha de la ocurrencia que se confirma (debe ser la próxima pendiente).
    public DateTime Fecha { get; set; }

    // Si se indica, reemplaza el monto de la plantilla solo para esta ocurrencia (recibos variables).
    public decimal? Monto { get; set; }
}

public class OmitirRecurrenteRequest
{
    public int RecurrenteId { get; set; }
    public DateTime Fecha { get; set; }
}

public static class FrecuenciaRecurrente
{
    public const string Semanal = "S";
    public const string Quincenal = "Q";
    public const string Mensual = "M";
    public const string Anual = "A";

    public static bool EsValida(string? frecuencia) =>
        frecuencia is Semanal or Quincenal or Mensual or Anual;

    public static string Nombre(string frecuencia) => frecuencia switch
    {
        Semanal => "Semanal",
        Quincenal => "Quincenal (15 y fin de mes)",
        Mensual => "Mensual",
        Anual => "Anual",
        _ => frecuencia
    };

    public static List<(string Clave, string Descripcion)> GetList() =>
    [
        (Semanal, Nombre(Semanal)),
        (Quincenal, Nombre(Quincenal)),
        (Mensual, Nombre(Mensual)),
        (Anual, Nombre(Anual))
    ];

    // Ocurrencia siguiente a `actual`. `inicio` fija el día del mes (mensual y anual) para que no se desplace:
    // una renta del 31 cae el 28 en febrero pero vuelve al 31 en marzo.
    public static DateTime Siguiente(DateTime actual, DateTime inicio, string frecuencia)
    {
        actual = actual.Date;
        switch (frecuencia)
        {
            case Semanal:
                return actual.AddDays(7);

            case Quincenal:
            {
                var ultimo = DateTime.DaysInMonth(actual.Year, actual.Month);
                if (actual.Day < 15) return new DateTime(actual.Year, actual.Month, 15);
                if (actual.Day < ultimo) return new DateTime(actual.Year, actual.Month, ultimo);
                var siguienteMes = new DateTime(actual.Year, actual.Month, 1).AddMonths(1);
                return new DateTime(siguienteMes.Year, siguienteMes.Month, 15);
            }

            case Mensual:
            {
                var siguienteMes = new DateTime(actual.Year, actual.Month, 1).AddMonths(1);
                return Dia(siguienteMes.Year, siguienteMes.Month, inicio.Day);
            }

            case Anual:
                return Dia(actual.Year + 1, inicio.Month, inicio.Day);

            default:
                throw new ArgumentException("Frecuencia inválida", nameof(frecuencia));
        }
    }

    private static DateTime Dia(int anio, int mes, int dia) =>
        new(anio, mes, Math.Min(dia, DateTime.DaysInMonth(anio, mes)));
}
