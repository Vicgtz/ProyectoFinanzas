using Finanzas.Models.Categoria;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Operacion;
using Finanzas.Models.Persona;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Components;

public partial class MovimientoForm
{
    // Operación a editar; null para crear una nueva.
    [Parameter] public OperacionModel? Operacion { get; set; }
    // Con Operacion informada, precarga sus datos pero guarda un movimiento nuevo (fecha de hoy).
    [Parameter] public bool Copiar { get; set; }
    // Tipo y persona con los que se abre el formulario al crear (por ejemplo, desde el detalle de una persona).
    [Parameter] public string? TipoInicial { get; set; }
    [Parameter] public int? PersonaIdInicial { get; set; }
    [Parameter] public EventCallback OnGuardado { get; set; }
    [Parameter] public EventCallback OnCancelado { get; set; }

    private readonly record struct TipoItem(string Clave, string Texto);

    private static readonly TipoItem[] Tipos =
    [
        new(TipoOperacion.Gasto, "Gasto"),
        new(TipoOperacion.Ingreso, "Ingreso"),
        new(TipoOperacion.Transferencia, "Transferencia entre cuentas"),
        new(TipoOperacion.PagoTarjeta, "Pago de tarjeta"),
        new(TipoOperacion.PrestamoOtorgado, "Préstamo que di"),
        new(TipoOperacion.DeudaAdquirida, "Me prestaron dinero"),
        new(TipoOperacion.CobroPrestamo, "Abono recibido de un préstamo"),
        new(TipoOperacion.PagoDeudaPersona, "Pago de una deuda")
    ];

    private List<CuentaModel> cuentas = [];
    private List<CategoriaModel> categorias = [];
    private List<PersonaModel> personas = [];
    private OperacionRequest form = new() { TipoOperacion = TipoOperacion.Gasto };
    private DateTime? fecha = DateTime.Today;
    private bool cargando = true;
    private bool guardando;
    private string? errorForm;
    private bool esMsi;
    private int mesesMsi = 6;

    private static readonly int[] MesesComunes = [3, 6, 9, 12, 15, 18, 24, 36];

    private bool EsEdicion => Operacion != null && !Copiar;
    private bool OrigenEsCredito => cuentas.Any(c => c.Id == form.CuentaOrigenId && EsCredito(c));
    private bool PermiteMsi => form.TipoOperacion == TipoOperacion.Gasto && OrigenEsCredito;
    private IEnumerable<int> OpcionesMeses => MesesComunes.Contains(mesesMsi) ? MesesComunes : MesesComunes.Append(mesesMsi).Order();

    private bool PermiteAfectarCuenta => form.TipoOperacion is TipoOperacion.PrestamoOtorgado or TipoOperacion.DeudaAdquirida;
    private bool SinCuenta => PermiteAfectarCuenta && !form.AfectaCuenta;
    private bool PideOrigen => !SinCuenta && form.TipoOperacion is TipoOperacion.Gasto or TipoOperacion.Transferencia or TipoOperacion.PagoTarjeta or TipoOperacion.PrestamoOtorgado or TipoOperacion.PagoDeudaPersona;
    private bool PideDestino => !SinCuenta && form.TipoOperacion is TipoOperacion.Ingreso or TipoOperacion.Transferencia or TipoOperacion.PagoTarjeta or TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo;
    private bool PidePersona => TipoOperacionTexto.EsPersona(form.TipoOperacion);
    private bool PideCategoria => form.TipoOperacion is TipoOperacion.Gasto or TipoOperacion.Ingreso;
    private bool EsTransferenciaOPago => form.TipoOperacion is TipoOperacion.Transferencia or TipoOperacion.PagoTarjeta;

    // El gasto puede salir de cualquier cuenta (también tarjeta); transferencias y pagos solo desde débito/efectivo.
    private IEnumerable<CuentaModel> CuentasOrigen =>
        Elegibles(form.CuentaOrigenId, c => form.TipoOperacion == TipoOperacion.Gasto || !EsCredito(c));

    private IEnumerable<CuentaModel> CuentasDestino =>
        Elegibles(form.CuentaDestinoId, c => form.TipoOperacion == TipoOperacion.PagoTarjeta ? EsCredito(c) : !EsCredito(c));

    private IEnumerable<CategoriaModel> CategoriasVisibles
    {
        get
        {
            var tipo = form.TipoOperacion == TipoOperacion.Ingreso ? "I" : "G";
            return categorias.Where(c => c.TipoSugerido == tipo || c.Id == form.CategoriaId);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var rCuentas = await CuentaApi.GetList();
        var rCategorias = await CategoriaApi.GetList();
        var rPersonas = await PersonaApi.GetList();
        if (!rCuentas.Success) errorForm = rCuentas.Message;
        else if (!rCategorias.Success) errorForm = rCategorias.Message;
        cuentas = rCuentas.Data ?? [];
        categorias = rCategorias.Data ?? [];
        personas = rPersonas.Data ?? [];

        if (Operacion != null)
        {
            CargarOperacion(Operacion);
            if (Copiar)
            {
                form.Id = 0;
                fecha = DateTime.Today;
            }
        }
        else
        {
            if (TipoInicial != null) form.TipoOperacion = TipoInicial;
            form.PersonaId = PersonaIdInicial;
        }
        cargando = false;
    }

    private void CargarOperacion(OperacionModel op)
    {
        form = new OperacionRequest
        {
            Id = op.Id,
            TipoOperacion = op.TipoOperacion,
            Monto = op.Monto,
            Fecha = op.Fecha,
            Descripcion = op.Descripcion,
            CategoriaId = op.CategoriaId,
            PersonaId = op.PersonaId,
            AfectaCuenta = op.Movimientos.Count > 0
        };
        fecha = op.Fecha.Date;
        if (op.Msi != null)
        {
            esMsi = true;
            mesesMsi = op.Msi.NumeroMeses;
        }

        // Gasto e ingreso tienen una sola cuenta. En transferencias el origen es el importe negativo;
        // en pagos de tarjeta el origen es la cuenta que no es de crédito.
        foreach (var m in op.Movimientos)
        {
            var esOrigen = op.TipoOperacion switch
            {
                TipoOperacion.Gasto or TipoOperacion.PrestamoOtorgado or TipoOperacion.PagoDeudaPersona => true,
                TipoOperacion.Ingreso or TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo => false,
                TipoOperacion.PagoTarjeta => cuentas.FirstOrDefault(c => c.Id == m.CuentaId) is { } c && !EsCredito(c),
                _ => m.Importe < 0
            };
            if (esOrigen) form.CuentaOrigenId = m.CuentaId; else form.CuentaDestinoId = m.CuentaId;
        }
    }

    private IEnumerable<CuentaModel> Elegibles(int? seleccionada, Func<CuentaModel, bool> filtro) =>
        cuentas.Where(c => (c.Activa || c.Id == seleccionada) && filtro(c));

    private static bool EsCredito(CuentaModel c) => c.TipoCuenta == TipoCuentaItem.Credito;

    private void CambiarTipo(string tipo)
    {
        form.TipoOperacion = tipo;
        form.CuentaOrigenId = null;
        form.CuentaDestinoId = null;
        form.CategoriaId = null;
        form.AfectaCuenta = true;
        esMsi = false;
    }

    private async Task Guardar()
    {
        errorForm = Validar();
        if (errorForm != null) return;

        // Se conserva la hora original al editar el mismo día; si no, la hora actual (sin pasar de "ahora").
        var hora = EsEdicion && Operacion!.Fecha.Date == fecha!.Value.Date ? Operacion.Fecha.TimeOfDay : DateTime.Now.TimeOfDay;
        form.Fecha = fecha!.Value.Date.Add(hora);
        if (form.Fecha > DateTime.Now) form.Fecha = DateTime.Now;
        if (!PideOrigen) form.CuentaOrigenId = null;
        if (!PideDestino) form.CuentaDestinoId = null;
        if (!PideCategoria) form.CategoriaId = null;
        if (!PidePersona) form.PersonaId = null;
        form.NumeroMeses = PermiteMsi && esMsi ? mesesMsi : null;

        guardando = true;
        var respuesta = EsEdicion ? await OperacionApi.Edit(form) : await OperacionApi.Save(form);
        guardando = false;

        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }
        await OnGuardado.InvokeAsync();
    }

    private Task Cancelar() => guardando ? Task.CompletedTask : OnCancelado.InvokeAsync();

    // Validación rápida en pantalla; la API vuelve a validar todo.
    private string? Validar()
    {
        if (form.Monto <= 0) return "El monto debe ser mayor a cero";
        if (fecha == null) return "Ingresa la fecha";
        if (fecha.Value.Date > DateTime.Today) return "No se permiten fechas futuras";
        if (PidePersona && form.PersonaId == null) return "Selecciona la persona";
        if (PideOrigen && form.CuentaOrigenId == null) return "Selecciona la cuenta";
        if (PideDestino && form.CuentaDestinoId == null) return "Selecciona la cuenta";
        if (form.TipoOperacion == TipoOperacion.Transferencia && form.CuentaOrigenId == form.CuentaDestinoId)
            return "El origen y el destino deben ser distintos";
        return null;
    }

    private IEnumerable<PersonaModel> PersonasVisibles => personas.Where(p => p.Activa || p.Id == form.PersonaId);
}
