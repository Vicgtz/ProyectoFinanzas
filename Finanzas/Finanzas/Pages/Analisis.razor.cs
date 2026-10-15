using System.Globalization;
using Finanzas.Models.Analisis;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Analisis
{
    [Inject] private AnalisisService AnalisisApi { get; set; } = default!;

    private AnalisisModel? datos;
    private bool cargando = true;
    private string? error;

    private decimal monto;
    private int mesesCompra = 1;
    private ImpactoCompraModel? impacto;
    private bool simulando;
    private string? errorImpacto;

    // Se omiten los meses vacíos anteriores al primer movimiento.
    private List<AnalisisMesModel> MesesVisibles =>
        datos == null ? [] : datos.Meses.SkipWhile(m => m.Ingresos == 0 && m.Gastos == 0).DefaultIfEmpty(datos.Meses[^1]).ToList();

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await AnalisisApi.Get();
        if (respuesta.Success)
            datos = respuesta.Data;
        else
            error = respuesta.Message;
        cargando = false;
    }

    private async Task Simular()
    {
        errorImpacto = null;
        if (monto <= 0)
        {
            errorImpacto = "Ingresa el monto de la compra";
            return;
        }

        simulando = true;
        var respuesta = await AnalisisApi.GetImpacto(monto, mesesCompra);
        simulando = false;

        if (respuesta.Success)
            impacto = respuesta.Data;
        else
            errorImpacto = respuesta.Message;
    }

    // Altura relativa al mayor valor de la serie (ingreso o gasto), con un mínimo para que se vea la barra.
    private string Altura(decimal valor)
    {
        var max = datos!.Meses.Max(m => Math.Max(m.Ingresos, m.Gastos));
        if (max == 0 || valor == 0) return "0";
        return Math.Max(3, valor / max * 100).ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Peso de cada segmento dentro de la barra apilada del gasto (flex-grow).
    private static string Num(decimal valor) => Math.Max(valor, 0).ToString("0.##", CultureInfo.InvariantCulture);

    private static string NombreMes(int mes, int anio) =>
        Formato.Mexico.TextInfo.ToTitleCase(new DateTime(anio, mes, 1).ToString("MMMM yyyy", Formato.Mexico));

    private static string NombreMesCorto(int mes) =>
        Formato.Mexico.TextInfo.ToTitleCase(new DateTime(2000, mes, 1).ToString("MMM", Formato.Mexico).TrimEnd('.'));

    private static string NombreClasificacion(string c) => c switch
    {
        "E" => "Esencial",
        "N" => "No esencial",
        _ => "Otro"
    };

    private static string TituloVeredicto(string nivel) => nivel switch
    {
        NivelAlerta.Bien => "Viable",
        NivelAlerta.Aviso => "Con precaución",
        NivelAlerta.Alerta => "No recomendable por ahora",
        _ => "Resultado"
    };

    private static string Colchon(decimal? meses) => meses == null ? "—" : $"{meses:0.#}";

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
