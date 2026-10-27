using System.Globalization;
using Finanzas.Models.Categoria;
using Finanzas.Models.Presupuesto;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Presupuestos
{
    private static readonly CultureInfo Cultura = new("es-MX");

    [Inject] private PresupuestoService PresupuestoApi { get; set; } = default!;
    [Inject] private CategoriaService CategoriaApi { get; set; } = default!;

    private PresupuestoResumenModel? resumen;
    private List<CategoriaModel> categorias = [];
    private bool cargando = true;
    private string? error;
    private int anio = DateTime.Today.Year;
    private int mes = DateTime.Today.Month;

    private bool modalAbierto;
    private bool editando;
    private bool guardando;
    private string? errorForm;
    private string nombreEditando = "";
    private PresupuestoModel form = new();

    private PresupuestoItemModel? aEliminar;
    private bool eliminando;
    private string? errorEliminar;

    private bool EsMesActual => anio == DateTime.Today.Year && mes == DateTime.Today.Month;

    // Categorías de gasto que todavía no tienen presupuesto (una por categoría).
    private IEnumerable<CategoriaModel> CategoriasLibres =>
        categorias.Where(c => c.Activa && c.TipoSugerido != "I" && (resumen == null || resumen.Items.All(i => i.CategoriaId != c.Id)));

    protected override async Task OnInitializedAsync()
    {
        var respuesta = await CategoriaApi.GetList();
        if (respuesta.Success) categorias = respuesta.Data ?? [];
        await Cargar();
    }

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await PresupuestoApi.GetResumen(anio, mes);
        if (respuesta.Success)
            resumen = respuesta.Data;
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

    private void Nuevo()
    {
        editando = false;
        errorForm = null;
        form = new PresupuestoModel();
        modalAbierto = true;
    }

    private void Editar(PresupuestoItemModel item)
    {
        editando = true;
        errorForm = null;
        nombreEditando = item.Categoria;
        form = new PresupuestoModel { Id = item.Id, CategoriaId = item.CategoriaId, Monto = item.Monto };
        modalAbierto = true;
    }

    private void CerrarModal()
    {
        if (!guardando) modalAbierto = false;
    }

    private async Task Guardar()
    {
        if (!editando && form.CategoriaId == 0) { errorForm = "Selecciona la categoría"; return; }
        if (form.Monto <= 0) { errorForm = "El monto debe ser mayor a cero"; return; }

        guardando = true;
        var respuesta = editando ? await PresupuestoApi.Edit(form) : await PresupuestoApi.Save(form);
        guardando = false;

        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }

        modalAbierto = false;
        await Cargar();
    }

    private void PedirEliminar(PresupuestoItemModel item)
    {
        errorEliminar = null;
        aEliminar = item;
    }

    private async Task Eliminar()
    {
        if (aEliminar == null) return;
        eliminando = true;
        var respuesta = await PresupuestoApi.Delete(aEliminar.Id);
        eliminando = false;

        if (!respuesta.Success)
        {
            errorEliminar = respuesta.Message;
            return;
        }
        aEliminar = null;
        await Cargar();
    }

    private static string NombreMes(int anio, int mes) =>
        Cultura.TextInfo.ToTitleCase(new DateTime(anio, mes, 1).ToString("MMMM yyyy", Cultura));

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
