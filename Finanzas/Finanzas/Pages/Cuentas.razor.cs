using Finanzas.Models.Cuenta;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Cuentas
{
    [Inject] private CuentaService CuentaApi { get; set; } = default!;

    private List<CuentaModel> cuentas = [];
    private bool cargando = true;
    private string? error;

    private bool mostrarInactivas;

    private bool modalAbierto;
    private bool editando;
    private bool guardando;
    private string? errorForm;
    private CuentaModel form = new();
    private int? diaCorte; // InputNumber no soporta byte

    private static readonly List<TipoCuentaItem> Tipos = TipoCuentaItem.GetList();

    private List<CuentaModel> CuentasVisibles =>
        cuentas.Where(c => mostrarInactivas || c.Activa).ToList();

    private decimal TotalDisponible =>
        cuentas.Where(c => c.Activa && !EsCredito(c)).Sum(c => c.Saldo);

    private decimal TotalDeuda =>
        cuentas.Where(c => c.Activa && EsCredito(c)).Sum(c => c.Saldo);

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await CuentaApi.GetList();
        if (respuesta.Success)
            cuentas = respuesta.Data ?? [];
        else
            error = respuesta.Message;
        cargando = false;
    }

    private void Nueva()
    {
        editando = false;
        errorForm = null;
        form = new CuentaModel { TipoCuenta = TipoCuentaItem.Debito, Activa = true };
        diaCorte = null;
        modalAbierto = true;
    }

    private void Editar(CuentaModel cuenta)
    {
        editando = true;
        errorForm = null;
        form = new CuentaModel
        {
            Id = cuenta.Id,
            Nombre = cuenta.Nombre,
            TipoCuenta = cuenta.TipoCuenta,
            Saldo = cuenta.Saldo,
            LimiteCredito = cuenta.LimiteCredito,
            DiaCorte = cuenta.DiaCorte,
            Activa = cuenta.Activa
        };
        diaCorte = cuenta.DiaCorte;
        modalAbierto = true;
    }

    private void CerrarModal()
    {
        if (!guardando) modalAbierto = false;
    }

    private async Task Guardar()
    {
        errorForm = Validar();
        if (errorForm != null) return;

        guardando = true;
        var respuesta = editando ? await CuentaApi.Edit(form) : await CuentaApi.Save(form);
        guardando = false;

        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }

        modalAbierto = false;
        await Cargar();
    }

    private async Task CambiarEstado(CuentaModel cuenta)
    {
        error = null;
        var respuesta = await CuentaApi.Edit(new CuentaModel
        {
            Id = cuenta.Id,
            Nombre = cuenta.Nombre,
            TipoCuenta = cuenta.TipoCuenta,
            Saldo = cuenta.Saldo,
            LimiteCredito = cuenta.LimiteCredito,
            DiaCorte = cuenta.DiaCorte,
            Activa = !cuenta.Activa
        });

        if (!respuesta.Success)
        {
            error = respuesta.Message;
            return;
        }
        await Cargar();
    }

    // Validación rápida en pantalla; la API vuelve a validar todo.
    private string? Validar()
    {
        if (string.IsNullOrWhiteSpace(form.Nombre)) return "Ingresa el nombre de la cuenta";
        if (!editando && form.Saldo < 0) return "El saldo no puede ser negativo";

        if (form.TipoCuenta == TipoCuentaItem.Credito)
        {
            if (form.LimiteCredito is null or < 0) return "Ingresa un límite de crédito válido";
            if (diaCorte is null or < 1 or > 31) return "El día de corte debe estar entre 1 y 31";
            form.DiaCorte = (byte)diaCorte;
            if (!editando && form.Saldo > form.LimiteCredito) return "La deuda inicial no puede superar el límite";
        }
        else
        {
            form.LimiteCredito = null;
            form.DiaCorte = null;
        }
        return null;
    }

    private static bool EsCredito(CuentaModel c) => c.TipoCuenta == TipoCuentaItem.Credito;

    private static string NombreTipo(string clave) =>
        Tipos.FirstOrDefault(t => t.Clave == clave)?.Descripcion ?? clave;

    private static string Moneda(decimal valor) =>
        valor < 0 ? $"-${Math.Abs(valor):N2}" : $"${valor:N2}";
}
