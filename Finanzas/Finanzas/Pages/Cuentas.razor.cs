using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Cuentas
{
    [Inject] private CuentaService CuentaApi { get; set; } = default!;
    [Inject] private OperacionService OperacionApi { get; set; } = default!;

    private List<OperacionModel> recientes = [];
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
    private int? diasPago;

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
        var tareaCuentas = CuentaApi.GetList();
        var tareaRecientes = OperacionApi.GetList(new OperacionFiltro { Take = 5 });
        await Task.WhenAll(tareaCuentas, tareaRecientes);

        var respuesta = tareaCuentas.Result;
        if (respuesta.Success)
            cuentas = respuesta.Data ?? [];
        else
            error = respuesta.Message;

        // Los movimientos recientes son un complemento: si fallan, la pantalla sigue funcionando.
        recientes = tareaRecientes.Result.Success ? tareaRecientes.Result.Data ?? [] : [];
        cargando = false;
    }

    private decimal CreditoDisponible =>
        cuentas.Where(c => c.Activa && EsCredito(c)).Sum(c => (c.LimiteCredito ?? 0) - c.Saldo);

    // Cuentas de dinero propio (efectivo y débito) activas, para la distribución.
    private List<CuentaModel> Propias => cuentas.Where(c => c.Activa && !EsCredito(c) && c.Saldo > 0).ToList();

    private decimal Porcentaje(CuentaModel c) => TotalDisponible == 0 ? 0 : c.Saldo / TotalDisponible * 100;

    private static string Color(int i) => $"var(--fz-cat-{i % 8 + 1})";

    private static string Num(decimal valor) => valor.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    // Porcentaje de uso del límite (0 a 100) para la barra de una tarjeta.
    private static string Uso(CuentaModel c) =>
        c.LimiteCredito is > 0 ? Num(Math.Clamp(c.Saldo / c.LimiteCredito.Value * 100, 0, 100)) : "0";

    private static string ClaseUso(CuentaModel c)
    {
        var uso = c.LimiteCredito is > 0 ? c.Saldo / c.LimiteCredito.Value * 100 : 0;
        return uso > 60 ? "bg-danger" : uso > 30 ? "bg-warning" : "bg-success";
    }

    private static string IconoCuenta(CuentaModel c) => c.TipoCuenta switch
    {
        TipoCuentaItem.Efectivo => "billete",
        TipoCuentaItem.Debito => "cuentas",
        _ => "tarjeta"
    };

    private static string ClaseCuenta(CuentaModel c) => c.TipoCuenta switch
    {
        TipoCuentaItem.Efectivo => "cuenta-efectivo",
        TipoCuentaItem.Debito => "cuenta-debito",
        _ => "cuenta-credito"
    };

    // Barras del resumen: efectivo y débito con su saldo, tarjetas con su deuda.
    private decimal MaximoBarras => Math.Max(1, CuentasVisibles.Select(c => c.Saldo).DefaultIfEmpty(0).Max());

    private string Altura(CuentaModel c) => Num(Math.Max(3, c.Saldo / MaximoBarras * 100));

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

    private void Nueva()
    {
        editando = false;
        errorForm = null;
        form = new CuentaModel { TipoCuenta = TipoCuentaItem.Debito, Activa = true };
        diaCorte = null;
        diasPago = null;
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
            DiasParaPago = cuenta.DiasParaPago,
            Activa = cuenta.Activa
        };
        diaCorte = cuenta.DiaCorte;
        diasPago = cuenta.DiasParaPago;
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
            DiasParaPago = cuenta.DiasParaPago,
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
        if (form.Saldo < 0) return "El saldo no puede ser negativo";

        if (form.TipoCuenta == TipoCuentaItem.Credito)
        {
            if (form.LimiteCredito is null or < 0) return "Ingresa un límite de crédito válido";
            if (diaCorte is null or < 1 or > 31) return "El día de corte debe estar entre 1 y 31";
            form.DiaCorte = (byte)diaCorte;
            if (diasPago is < 1 or > 60) return "Los días para pago deben estar entre 1 y 60";
            form.DiasParaPago = diasPago == null ? null : (byte)diasPago;
            if (form.Saldo > form.LimiteCredito) return "La deuda no puede superar el límite";
        }
        else
        {
            form.LimiteCredito = null;
            form.DiaCorte = null;
            form.DiasParaPago = null;
        }
        return null;
    }

    private static bool EsCredito(CuentaModel c) => c.TipoCuenta == TipoCuentaItem.Credito;

    private static string NombreTipo(string clave) =>
        Tipos.FirstOrDefault(t => t.Clave == clave)?.Descripcion ?? clave;

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
