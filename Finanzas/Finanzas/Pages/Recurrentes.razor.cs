using Finanzas.Models.Categoria;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Finanzas.Models.Recurrente;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Recurrentes
{
    [Inject] private RecurrenteService RecurrenteApi { get; set; } = default!;
    [Inject] private CuentaService CuentaApi { get; set; } = default!;
    [Inject] private CategoriaService CategoriaApi { get; set; } = default!;

    private List<RecurrenteModel> recurrentes = [];
    private List<CuentaModel> cuentas = [];
    private List<CategoriaModel> categorias = [];
    private bool cargando = true;
    private string? error;
    private bool mostrarInactivas;

    // Cambiar la llave vuelve a crear el componente de pendientes para que lea las fechas nuevas.
    private int versionPendientes;

    private bool modalAbierto;
    private bool editando;
    private bool guardando;
    private string? errorForm;
    private RecurrenteModel form = new();
    private DateTime proximaOriginal;

    private RecurrenteModel? aEliminar;
    private bool eliminando;
    private string? errorEliminar;

    private static readonly List<(string Clave, string Descripcion)> Frecuencias = FrecuenciaRecurrente.GetList();

    private List<RecurrenteModel> Visibles =>
        recurrentes.Where(r => mostrarInactivas || r.Activa).ToList();

    // Un ingreso no puede entrar a una tarjeta de crédito.
    private IEnumerable<CuentaModel> CuentasDisponibles =>
        cuentas.Where(c => c.Activa && (form.TipoOperacion != TipoOperacion.Ingreso || c.TipoCuenta != TipoCuentaItem.Credito));

    private IEnumerable<CategoriaModel> CategoriasDisponibles =>
        categorias.Where(c => c.Activa && c.TipoSugerido == (form.TipoOperacion == TipoOperacion.Ingreso ? "I" : "G"));

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;

        var rRecurrentes = await RecurrenteApi.GetList();
        var rCuentas = await CuentaApi.GetList(soloActivas: true);
        var rCategorias = await CategoriaApi.GetList();

        if (!rRecurrentes.Success) error = rRecurrentes.Message;
        else if (!rCuentas.Success) error = rCuentas.Message;
        else if (!rCategorias.Success) error = rCategorias.Message;

        recurrentes = rRecurrentes.Data ?? [];
        cuentas = rCuentas.Data ?? [];
        categorias = rCategorias.Data ?? [];
        cargando = false;
    }

    private void Nueva()
    {
        editando = false;
        errorForm = null;
        form = new RecurrenteModel
        {
            TipoOperacion = TipoOperacion.Gasto,
            Frecuencia = FrecuenciaRecurrente.Mensual,
            FechaInicio = DateTime.Today,
            ProximaFecha = DateTime.Today,
            CuentaId = 0
        };
        modalAbierto = true;
    }

    private void Editar(RecurrenteModel r)
    {
        editando = true;
        errorForm = null;
        form = new RecurrenteModel
        {
            Id = r.Id,
            TipoOperacion = r.TipoOperacion,
            Descripcion = r.Descripcion,
            Monto = r.Monto,
            CategoriaId = r.CategoriaId,
            CuentaId = r.CuentaId,
            Frecuencia = r.Frecuencia,
            FechaInicio = r.FechaInicio,
            ProximaFecha = r.ProximaFecha,
            FechaFin = r.FechaFin,
            Activa = r.Activa
        };
        proximaOriginal = r.ProximaFecha.Date;
        modalAbierto = true;
    }

    private void CambiarTipo(string tipo)
    {
        form.TipoOperacion = tipo;
        // La categoría y la cuenta elegidas pueden no aplicar al tipo nuevo.
        form.CategoriaId = null;
        if (tipo == TipoOperacion.Ingreso && cuentas.Any(c => c.Id == form.CuentaId && c.TipoCuenta == TipoCuentaItem.Credito))
            form.CuentaId = 0;
    }

    private void CerrarModal()
    {
        if (!guardando) modalAbierto = false;
    }

    private async Task Guardar()
    {
        errorForm = Validar();
        if (errorForm != null) return;

        // El día de la próxima fecha es el que se repite en mensual y anual.
        // Al crear, o si el usuario cambió la fecha, esa fecha fija el día que se repite; si no, se conserva el ancla
        // (una renta del 31 que cayó el 28 de febrero sigue siendo del 31).
        if (!editando || form.ProximaFecha.Date != proximaOriginal) form.FechaInicio = form.ProximaFecha;

        guardando = true;
        var respuesta = editando ? await RecurrenteApi.Edit(form) : await RecurrenteApi.Save(form);
        guardando = false;

        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }

        modalAbierto = false;
        await Recargar();
    }

    private async Task CambiarEstado(RecurrenteModel r)
    {
        error = null;
        var respuesta = await RecurrenteApi.Edit(new RecurrenteModel
        {
            Id = r.Id,
            TipoOperacion = r.TipoOperacion,
            Descripcion = r.Descripcion,
            Monto = r.Monto,
            CategoriaId = r.CategoriaId,
            CuentaId = r.CuentaId,
            Frecuencia = r.Frecuencia,
            FechaInicio = r.FechaInicio,
            ProximaFecha = r.ProximaFecha,
            FechaFin = r.FechaFin,
            Activa = !r.Activa
        });

        if (!respuesta.Success)
        {
            error = respuesta.Message;
            return;
        }
        await Recargar();
    }

    private void PedirEliminar(RecurrenteModel r)
    {
        errorEliminar = null;
        aEliminar = r;
    }

    private async Task Eliminar()
    {
        if (aEliminar == null) return;
        eliminando = true;
        var respuesta = await RecurrenteApi.Delete(aEliminar.Id);
        eliminando = false;

        if (!respuesta.Success)
        {
            errorEliminar = respuesta.Message;
            return;
        }
        aEliminar = null;
        await Recargar();
    }

    private async Task Recargar()
    {
        versionPendientes++;
        await Cargar();
    }

    // Validación rápida en pantalla; la API vuelve a validar todo.
    private string? Validar()
    {
        if (string.IsNullOrWhiteSpace(form.Descripcion)) return "Ingresa una descripción";
        if (form.Monto <= 0) return "El monto debe ser mayor a cero";
        if (form.CuentaId == 0) return "Selecciona la cuenta";
        if (form.FechaFin != null && form.FechaFin.Value.Date < form.ProximaFecha.Date) return "La fecha final no puede ser anterior a la primera fecha";
        return null;
    }

    private static bool Terminada(RecurrenteModel r) => r.FechaFin != null && r.ProximaFecha.Date > r.FechaFin.Value.Date;

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
