using Finanzas.Models.Operacion;
using Finanzas.Models.Persona;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class PersonaDetalle
{
    private const int PorPagina = 50;

    [Parameter] public int Id { get; set; }

    [Inject] private PersonaService PersonaApi { get; set; } = default!;
    [Inject] private OperacionService OperacionApi { get; set; } = default!;

    private PersonaModel? persona;
    private List<OperacionModel> operaciones = [];
    private bool cargando = true;
    private bool cargandoMas;
    private bool hayMas;
    private string? error;

    private bool formAbierto;
    private OperacionModel? enEdicion;
    private string? tipoNuevo;

    private OperacionModel? aEliminar;
    private bool eliminando;
    private string? errorEliminar;

    protected override async Task OnParametersSetAsync()
    {
        persona = null;
        await Cargar();
    }

    private async Task Cargar()
    {
        cargando = true;
        error = null;

        var rPersona = await PersonaApi.Get(Id);
        if (!rPersona.Success)
        {
            error = rPersona.Message;
            cargando = false;
            return;
        }
        persona = rPersona.Data;

        var rOperaciones = await OperacionApi.GetList(new OperacionFiltro { PersonaId = Id, Take = PorPagina });
        if (rOperaciones.Success)
        {
            operaciones = rOperaciones.Data ?? [];
            hayMas = operaciones.Count == PorPagina;
        }
        else
        {
            error = rOperaciones.Message;
        }
        cargando = false;
    }

    private async Task CargarMas()
    {
        cargandoMas = true;
        var respuesta = await OperacionApi.GetList(new OperacionFiltro { PersonaId = Id, Skip = operaciones.Count, Take = PorPagina });
        if (respuesta.Success)
        {
            var nuevas = respuesta.Data ?? [];
            operaciones.AddRange(nuevas);
            hayMas = nuevas.Count == PorPagina;
        }
        else
        {
            error = respuesta.Message;
        }
        cargandoMas = false;
    }

    private void Abrir(string tipo)
    {
        enEdicion = null;
        tipoNuevo = tipo;
        formAbierto = true;
    }

    private void Editar(OperacionModel operacion)
    {
        enEdicion = operacion;
        tipoNuevo = null;
        formAbierto = true;
    }

    private async Task Guardado()
    {
        formAbierto = false;
        await Cargar();
    }

    private void PedirEliminar(OperacionModel operacion)
    {
        errorEliminar = null;
        aEliminar = operacion;
    }

    private async Task Eliminar()
    {
        if (aEliminar == null) return;
        eliminando = true;
        var respuesta = await OperacionApi.Delete(aEliminar.Id);
        eliminando = false;

        if (!respuesta.Success)
        {
            errorEliminar = respuesta.Message;
            return;
        }
        aEliminar = null;
        await Cargar();
    }

    // Entra dinero con una deuda adquirida o un abono recibido; sale con un préstamo o un pago.
    private static string Signo(OperacionModel o) =>
        o.TipoOperacion is TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo ? "+" : "-";

    private static string Moneda(decimal valor) => $"${valor:N2}";
}
