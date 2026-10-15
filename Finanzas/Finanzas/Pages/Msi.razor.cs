using System.Globalization;
using Finanzas.Models.Msi;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Msi
{
    private static readonly CultureInfo Cultura = new("es-MX");

    [Inject] private MsiService MsiApi { get; set; } = default!;

    private MsiResumenModel? datos;
    private bool cargando = true;
    private string? error;
    private bool mostrarLiquidadas;

    private List<MsiPlanModel> PlanesVisibles =>
        datos?.Planes.Where(p => mostrarLiquidadas || p.MensualidadesRestantes > 0).ToList() ?? [];

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await MsiApi.GetResumen();
        if (respuesta.Success)
            datos = respuesta.Data;
        else
            error = respuesta.Message;
        cargando = false;
    }

    private static string Porcentaje(MsiPlanModel p) =>
        (p.NumeroMeses == 0 ? 0 : (decimal)p.MensualidadesTranscurridas / p.NumeroMeses * 100).ToString("0.##", CultureInfo.InvariantCulture);

    // Altura relativa al mes con más compromisos, con un mínimo para que se vea la barra.
    private string Altura(decimal importe)
    {
        var max = datos!.Calendario.Max(m => m.Importe);
        if (max == 0 || importe == 0) return "0";
        return Math.Max(4, importe / max * 100).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string NombreMes(int mes) =>
        Cultura.TextInfo.ToTitleCase(new DateTime(2000, mes, 1).ToString("MMM", Cultura).TrimEnd('.'));

    private static string Corto(decimal valor) =>
        valor >= 1000 ? $"{valor / 1000:0.#}k" : valor.ToString("0");

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
