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
        var respuesta = await CategoriaApi.GetList(incluirInactivas: true);
        if (respuesta.Success)
            categorias = respuesta.Data ?? [];
        else
            error = respuesta.Message;
        cargando = false;
    }

    // Formulario de alta/edición (solo categorías propias)
    private CategoriaModel form = new();
    private bool modalAbierto;
    private bool editando;
    private bool guardando;
    private string? errorForm;

    private void Nueva()
    {
        form = new CategoriaModel { TipoSugerido = "G", ClasificacionGasto = "X", Activa = true };
        editando = false;
        errorForm = null;
        modalAbierto = true;
    }

    private void Editar(CategoriaModel c)
    {
        form = new CategoriaModel
        {
            Id = c.Id, Nombre = c.Nombre, Emoji = c.Emoji, TipoSugerido = c.TipoSugerido,
            ClasificacionGasto = c.ClasificacionGasto, Activa = c.Activa, Orden = c.Orden, EsPropia = true
        };
        editando = true;
        errorForm = null;
        modalAbierto = true;
    }

    private void CerrarModal()
    {
        if (!guardando) modalAbierto = false;
    }

    private async Task Guardar()
    {
        if (guardando) return;
        errorForm = null;
        guardando = true;
        var respuesta = editando ? await CategoriaApi.Edit(form) : await CategoriaApi.Save(form);
        guardando = false;
        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }
        modalAbierto = false;
        await Cargar();
    }

    // Activar/desactivar sin abrir el formulario
    private async Task CambiarActiva(CategoriaModel c)
    {
        var copia = new CategoriaModel
        {
            Id = c.Id, Nombre = c.Nombre, Emoji = c.Emoji, TipoSugerido = c.TipoSugerido,
            ClasificacionGasto = c.ClasificacionGasto, Activa = !c.Activa, Orden = c.Orden
        };
        var respuesta = await CategoriaApi.Edit(copia);
        if (!respuesta.Success)
        {
            error = respuesta.Message;
            return;
        }
        await Cargar();
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
