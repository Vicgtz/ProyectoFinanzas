using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class CuentaDetalle
{
    private const int PorPagina = 50;

    [Parameter] public int Id { get; set; }

    [Inject] private CuentaService CuentaApi { get; set; } = default!;
    [Inject] private OperacionService OperacionApi { get; set; } = default!;

    private CuentaDetalleModel? detalle;
    private List<OperacionModel> operaciones = [];
    private bool cargando = true;
    private bool cargandoMas;
    private bool hayMas;
    private string? error;

    private bool registrando;
    private string? errorCorte;
    private bool movimientoAbierto;

    private bool EsCredito => detalle?.Cuenta.TipoCuenta == TipoCuentaItem.Credito;

    private decimal UsoCredito
    {
        get
        {
            var limite = detalle?.Cuenta.LimiteCredito ?? 0;
            return limite == 0 ? 0 : (detalle!.Cuenta.Saldo / limite * 100);
        }
    }

    private string UsoCreditoCss => Math.Clamp(UsoCredito, 0, 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    protected override async Task OnParametersSetAsync()
    {
        detalle = null;
        await Cargar();
    }

    private async Task Cargar()
    {
        cargando = true;
        error = null;

        var rDetalle = await CuentaApi.GetDetalle(Id);
        if (!rDetalle.Success)
        {
            error = rDetalle.Message;
            cargando = false;
            return;
        }
        detalle = rDetalle.Data;

        var rOperaciones = await OperacionApi.GetList(new OperacionFiltro { CuentaId = Id, Take = PorPagina });
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
        var respuesta = await OperacionApi.GetList(new OperacionFiltro { CuentaId = Id, Skip = operaciones.Count, Take = PorPagina });
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

    private async Task RegistrarCorte()
    {
        registrando = true;
        errorCorte = null;
        var respuesta = await CuentaApi.RegistrarCorte(new CorteRequest { CuentaId = Id });
        registrando = false;

        if (!respuesta.Success)
        {
            errorCorte = respuesta.Message;
            return;
        }
        await Cargar();
    }

    private async Task EliminarCorte(CorteModel corte)
    {
        errorCorte = null;
        var respuesta = await CuentaApi.EliminarCorte(corte.Id);
        if (!respuesta.Success)
        {
            errorCorte = respuesta.Message;
            return;
        }
        await Cargar();
    }

    private async Task Guardado()
    {
        movimientoAbierto = false;
        await Cargar();
    }

    // Cambio que la operación aplicó al saldo de esta cuenta.
    private decimal Importe(OperacionModel o) => o.Movimientos.Where(m => m.CuentaId == Id).Sum(m => m.Importe);

    // En tarjetas el saldo es deuda: subir es malo y bajar es bueno; en las demás cuentas es al revés.
    private string ClaseImporte(decimal importe) => importe == 0 ? "" : (importe > 0) == !EsCredito ? "text-success" : "text-danger";

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
