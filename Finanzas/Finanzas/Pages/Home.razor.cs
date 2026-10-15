using System.Globalization;
using Finanzas.Models.Dashboard;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Pages;

public partial class Home
{
    private static readonly CultureInfo Cultura = new("es-MX");
    private static readonly string[] Colores =
        ["#1b7f6b", "#f4b63f", "#d9534f", "#3d8bd9", "#8e6bbf", "#2fa37d", "#e07b39", "#8a9a95"];

    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private DashboardService DashboardApi { get; set; } = default!;

    private string Nombre { get; set; } = "";
    private bool movimientoAbierto;

    private DashboardModel? datos;
    private bool cargando = true;
    private string? error;
    private int anio = DateTime.Today.Year;
    private int mes = DateTime.Today.Month;

    private bool EsMesActual => anio == DateTime.Today.Year && mes == DateTime.Today.Month;

    protected override async Task OnInitializedAsync()
    {
        var estado = await AuthState.GetAuthenticationStateAsync();
        Nombre = estado.User.Identity?.Name ?? "";
        await Cargar();
    }

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await DashboardApi.Get(anio, mes);
        if (respuesta.Success)
            datos = respuesta.Data;
        else
            error = respuesta.Message;
        cargando = false;
    }

    private async Task CambiarMes(int delta)
    {
        var nuevo = new DateTime(anio, mes, 1).AddMonths(delta);
        if (nuevo > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) return;
        anio = nuevo.Year;
        mes = nuevo.Month;
        await Cargar();
    }

    private async Task Guardado()
    {
        movimientoAbierto = false;
        await Cargar();
    }

    // Comparación del mes consultado contra el mes anterior de la serie.
    private string? Comparacion()
    {
        if (datos is not { Meses.Count: >= 2 }) return null;
        var actual = datos.Meses[^1];
        var anterior = datos.Meses[^2];
        if (anterior.Gastos == 0) return actual.Gastos == 0 ? null : $"En {NombreMes(anterior.Mes, anterior.Anio)} no hubo gastos con qué comparar.";

        var cambio = (actual.Gastos - anterior.Gastos) / anterior.Gastos * 100;
        var direccion = cambio >= 0 ? "más" : "menos";
        return $"Gastaste {Math.Abs(cambio):0.#}% {direccion} que en {NombreMes(anterior.Mes, anterior.Anio)} ({Moneda(anterior.Gastos)}).";
    }

    // Altura relativa al valor mayor de toda la serie, con un mínimo para que se vea la barra.
    private string Altura(decimal valor)
    {
        var max = datos!.Meses.Max(m => Math.Max(m.Ingresos, m.Gastos));
        if (max == 0 || valor == 0) return "0";
        return Math.Max(3, valor / max * 100).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string Color(int i) => Colores[i % Colores.Length];

    private static string Num(decimal valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

    private static string NombreMes(int mes, int anio) =>
        Cultura.TextInfo.ToTitleCase(new DateTime(anio, mes, 1).ToString("MMMM yyyy", Cultura));

    private static string NombreMesCorto(int mes) =>
        Cultura.TextInfo.ToTitleCase(new DateTime(2000, mes, 1).ToString("MMM", Cultura).TrimEnd('.'));

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
