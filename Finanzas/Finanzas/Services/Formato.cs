using System.Globalization;

namespace Finanzas.Services;

public static class Formato
{
    public static readonly CultureInfo Mexico = new("es-MX");

    // Pesos mexicanos: $1,234.56 (negativos: -$1,234.56).
    public static string Moneda(decimal valor) =>
        valor < 0 ? $"-{Math.Abs(valor).ToString("C2", Mexico)}" : valor.ToString("C2", Mexico);
}
