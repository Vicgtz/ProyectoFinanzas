using System.Globalization;
using Finanzas.Models.Dashboard;
using Finanzas.Models.Operacion;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Finanzas.Pages;

public partial class Home
{
    private static readonly CultureInfo Cultura = new("es-MX");
    private static readonly string[] Colores =
        ["var(--fz-cat-1)", "var(--fz-cat-2)", "var(--fz-cat-3)", "var(--fz-cat-4)", "var(--fz-cat-5)", "var(--fz-cat-6)", "var(--fz-cat-7)", "var(--fz-cat-8)"];

    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private DashboardService DashboardApi { get; set; } = default!;
    [Inject] private CuentaService CuentaApi { get; set; } = default!;

    [Inject] private OperacionService OperacionApi { get; set; } = default!;

    private string Nombre { get; set; } = "";
    private bool movimientoAbierto;
    private List<OperacionModel> recientes = [];

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
        var tareaDatos = DashboardApi.Get(anio, mes);
        var tareaRecientes = OperacionApi.GetList(new OperacionFiltro { Take = 5 });
        await Task.WhenAll(tareaDatos, tareaRecientes);

        var respuesta = tareaDatos.Result;
        if (respuesta.Success)
            datos = respuesta.Data;
        else
            error = respuesta.Message;

        // Los movimientos recientes son un complemento: si fallan, el resto sigue funcionando.
        recientes = tareaRecientes.Result.Success ? tareaRecientes.Result.Data ?? [] : [];
        cargando = false;
    }

    private static string FechaHoy => "Hoy, " + DateTime.Today.ToString("d 'de' MMMM 'de' yyyy", Cultura);

    private static string IconoTipo(string t) => t switch
    {
        TipoOperacion.Ingreso => "flecha-abajo",
        TipoOperacion.Gasto => "flecha-arriba",
        TipoOperacion.Transferencia => "movimientos",
        TipoOperacion.PagoTarjeta => "cuentas",
        _ => "personas"
    };

    private static string ClaseMonto(string t) => t switch
    {
        TipoOperacion.Ingreso => "text-success",
        TipoOperacion.Gasto => "text-danger",
        _ => ""
    };

    private static string MontoReciente(OperacionModel o) => o.TipoOperacion switch
    {
        TipoOperacion.Ingreso or TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo => $"+{Moneda(o.Monto)}",
        TipoOperacion.Gasto or TipoOperacion.PrestamoOtorgado or TipoOperacion.PagoDeudaPersona => $"-{Moneda(o.Monto)}",
        _ => Moneda(o.Monto)
    };

    private static string TituloReciente(OperacionModel o) =>
        string.IsNullOrWhiteSpace(o.Descripcion) ? TipoOperacionTexto.Nombre(o.TipoOperacion) : o.Descripcion;

    // Texto del consejo: la comparación contra el mes anterior o, si no hay, uno general.
    private string Consejo() =>
        Comparacion() ?? "Mantén un equilibrio entre tus ingresos y gastos. Un control adecuado te acerca a tus metas.";

    private async Task MarcarPagado(int idCuenta)
    {
        var respuesta = await CuentaApi.MarcarPagado(idCuenta);
        if (!respuesta.Success)
        {
            error = respuesta.Message;
            return;
        }
        await Cargar();
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
