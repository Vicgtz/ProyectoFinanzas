using Finanzas.Models.Categoria;
using Finanzas.Models.Cuenta;
using Finanzas.Models.Dashboard;
using Finanzas.Models.Operacion;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Movimientos
{
    private const int PorPagina = 50;

    [Inject] private OperacionService OperacionApi { get; set; } = default!;
    [Inject] private CuentaService CuentaApi { get; set; } = default!;
    [Inject] private CategoriaService CategoriaApi { get; set; } = default!;
    [Inject] private DashboardService DashboardApi { get; set; } = default!;

    private DashboardModel? resumen;
    private string busqueda = "";
    private string pestana = "todos";

    private List<OperacionModel> operaciones = [];
    private List<CuentaModel> cuentas = [];
    private List<CategoriaModel> categorias = [];
    private bool cargando = true;
    private bool cargandoMas;
    private bool hayMas;
    private string? error;

    private string mes = DateTime.Today.ToString("yyyy-MM");
    private string tipo = "";
    private string cuentaId = "";
    private string categoriaId = "";

    private bool formAbierto;
    private OperacionModel? enEdicion;
    private bool copiando;

    private OperacionModel? aEliminar;
    private bool eliminando;
    private string? errorEliminar;

    protected override async Task OnInitializedAsync()
    {
        var rCuentas = await CuentaApi.GetList();
        var rCategorias = await CategoriaApi.GetList();
        cuentas = rCuentas.Data ?? [];
        categorias = rCategorias.Data ?? [];
        await Task.WhenAll(CargarResumen(), Recargar());
    }

    // Tarjetas de resumen: mismas cifras del dashboard para el mes elegido (no dependen de los demás filtros).
    private async Task CargarResumen()
    {
        var fecha = DateTime.TryParseExact(mes, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.Today;
        var respuesta = await DashboardApi.Get(fecha.Year, fecha.Month);
        resumen = respuesta.Success ? respuesta.Data : null;
    }

    private string NombreMes()
    {
        var fecha = DateTime.TryParseExact(mes, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : DateTime.Today;
        var cultura = Formato.Mexico;
        return cultura.TextInfo.ToTitleCase(fecha.ToString("MMMM yyyy", cultura));
    }

    // Pestañas y búsqueda se aplican sobre los movimientos ya cargados; el resto de filtros van al servidor.
    private List<OperacionModel> Visibles => operaciones
        .Where(CoincidePestana)
        .Where(CoincideBusqueda)
        .ToList();

    private bool CoincidePestana(OperacionModel o) => pestana switch
    {
        "ingresos" => o.TipoOperacion == TipoOperacion.Ingreso,
        "gastos" => o.TipoOperacion == TipoOperacion.Gasto,
        "prestamos" => o.TipoOperacion is TipoOperacion.PrestamoOtorgado or TipoOperacion.CobroPrestamo,
        "deudas" => o.TipoOperacion is TipoOperacion.DeudaAdquirida or TipoOperacion.PagoDeudaPersona,
        _ => true
    };

    private bool CoincideBusqueda(OperacionModel o)
    {
        var texto = busqueda.Trim();
        if (texto.Length == 0) return true;
        return new[] { o.Descripcion, o.CategoriaNombre, o.PersonaNombre, Cuentas(o), NombreTipo(o.TipoOperacion) }
            .Any(campo => campo != null && campo.Contains(texto, StringComparison.CurrentCultureIgnoreCase));
    }

    private static string IconoTipo(string t) => t switch
    {
        TipoOperacion.Ingreso => "flecha-abajo",
        TipoOperacion.Gasto => "flecha-arriba",
        TipoOperacion.Transferencia => "movimientos",
        TipoOperacion.PagoTarjeta => "cuentas",
        _ => "personas"
    };

    private OperacionFiltro Filtro(int skip)
    {
        DateTime? desde = null, hasta = null;
        if (DateTime.TryParseExact(mes, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var primerDia))
        {
            desde = primerDia;
            hasta = primerDia.AddMonths(1).AddDays(-1);
        }
        return Filtro(skip, desde, hasta);
    }

    private OperacionFiltro Filtro(int skip, DateTime? desde, DateTime? hasta) => new()
    {
        Desde = desde,
        Hasta = hasta,
        TipoOperacion = string.IsNullOrEmpty(tipo) ? null : tipo,
        CuentaId = int.TryParse(cuentaId, out var cu) ? cu : null,
        CategoriaId = int.TryParse(categoriaId, out var ca) ? ca : null,
        Skip = skip,
        Take = PorPagina
    };

    private async Task CambiarMes(ChangeEventArgs e)
    {
        mes = e.Value?.ToString() ?? "";
        await Task.WhenAll(CargarResumen(), Recargar());
    }

    private async Task Recargar()
    {
        cargando = true;
        error = null;
        var respuesta = await OperacionApi.GetList(Filtro(0));
        if (respuesta.Success)
        {
            operaciones = respuesta.Data ?? [];
            hayMas = operaciones.Count == PorPagina;
        }
        else
        {
            error = respuesta.Message;
        }
        cargando = false;
    }

    private async Task CargarMas()
    {
        cargandoMas = true;
        var respuesta = await OperacionApi.GetList(Filtro(operaciones.Count));
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

    private void Nuevo()
    {
        enEdicion = null;
        copiando = false;
        formAbierto = true;
    }

    private void Editar(OperacionModel operacion)
    {
        enEdicion = operacion;
        copiando = false;
        formAbierto = true;
    }

    private void Copiar(OperacionModel operacion)
    {
        enEdicion = operacion;
        copiando = true;
        formAbierto = true;
    }

    private void CerrarForm() => formAbierto = false;

    private async Task Guardado()
    {
        formAbierto = false;
        await Task.WhenAll(CargarResumen(), Recargar());
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
        await Task.WhenAll(CargarResumen(), Recargar());
    }

    // Una transferencia muestra origen → destino; el resto, la cuenta afectada.
    private static string Cuentas(OperacionModel o)
    {
        if (o.Movimientos.Count == 2)
        {
            var origen = o.Movimientos.FirstOrDefault(m => m.Importe < 0) ?? o.Movimientos[0];
            var destino = o.Movimientos.First(m => m != origen);
            return $"{origen.CuentaNombre} → {destino.CuentaNombre}";
        }
        return string.Join(", ", o.Movimientos.Select(m => m.CuentaNombre));
    }

    private static string NombreTipo(string t) => TipoOperacionTexto.Nombre(t);

    private static string ClaseTipo(string t) => t switch
    {
        TipoOperacion.Ingreso => "bg-success",
        TipoOperacion.Gasto => "bg-danger",
        TipoOperacion.PagoTarjeta => "bg-warning text-dark",
        TipoOperacion.PrestamoOtorgado or TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo or TipoOperacion.PagoDeudaPersona => "bg-primary",
        _ => "bg-secondary"
    };

    private static string ClaseMonto(string t) => t switch
    {
        TipoOperacion.Ingreso => "text-success",
        TipoOperacion.Gasto => "text-danger",
        _ => ""
    };

    private static string Monto(OperacionModel o) => o.TipoOperacion switch
    {
        TipoOperacion.Ingreso or TipoOperacion.DeudaAdquirida or TipoOperacion.CobroPrestamo => $"+{Moneda(o.Monto)}",
        TipoOperacion.Gasto or TipoOperacion.PrestamoOtorgado or TipoOperacion.PagoDeudaPersona => $"-{Moneda(o.Monto)}",
        _ => Moneda(o.Monto)
    };

    private static string Moneda(decimal valor) => Formato.Moneda(valor);
}
