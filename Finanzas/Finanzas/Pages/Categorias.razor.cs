using Finanzas.Models.Categoria;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Categorias
{
    [Inject] private CategoriaService CategoriaApi { get; set; } = default!;

    private List<CategoriaModel> categorias = [];
    private bool cargando = true;
    private string? error;

    private string filtroTipo = "";
    private string busqueda = "";

    private static readonly List<(string Clave, string Texto)> Filtros =
    [
        ("", "Todas"),
        ("I", "Ingresos"),
        ("G", "Gastos")
    ];

    private List<CategoriaModel> CategoriasVisibles =>
        categorias
            .Where(c => filtroTipo == "" || c.TipoSugerido == filtroTipo)
            .Where(c => string.IsNullOrWhiteSpace(busqueda) || c.Nombre.Contains(busqueda.Trim(), StringComparison.CurrentCultureIgnoreCase))
            .ToList();

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await CategoriaApi.GetList();
        if (respuesta.Success)
            categorias = respuesta.Data ?? [];
        else
            error = respuesta.Message;
        cargando = false;
    }

    private static string NombreTipo(string tipo) => tipo switch
    {
        "I" => "Ingreso",
        "G" => "Gasto",
        "A" => "Ambos",
        _ => tipo
    };

    private static string ClaseTipo(string tipo) => tipo switch
    {
        "I" => "bg-success",
        "G" => "bg-danger",
        _ => "bg-secondary"
    };

    // La clasificación solo aplica a gastos.
    private static string NombreClasificacion(CategoriaModel c)
    {
        if (c.TipoSugerido == "I") return "—";
        return c.ClasificacionGasto switch
        {
            "E" => "Esencial",
            "N" => "No esencial",
            "X" => "Otro",
            _ => c.ClasificacionGasto
        };
    }
}
