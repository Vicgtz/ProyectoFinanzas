namespace Finanzas.Models.Analisis;

public class AnalisisModel
{
    public List<AnalisisMesModel> Meses { get; set; } = [];

    // Meses con más gasto que ingreso dentro de los últimos 6 (incluye el mes en curso).
    public int MesesEnDeficit { get; set; }

    public List<AlertaModel> Alertas { get; set; } = [];
    public List<CategoriaCambioModel> CategoriasEnAumento { get; set; } = [];
}

public class AnalisisMesModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
    public decimal Esencial { get; set; }
    public decimal NoEsencial { get; set; }

    // Gastos de categorías clasificadas como "otro" y gastos sin categoría.
    public decimal Otros { get; set; }

    public decimal Diferencia => Ingresos - Gastos;
    public bool GastaMasQueIngresa => Gastos > Ingresos;

    // Variación del gasto contra el mes anterior de la serie; null si el mes anterior no tuvo gastos.
    public decimal? VariacionGastos { get; set; }
}

public static class NivelAlerta
{
    public const string Info = "info";
    public const string Aviso = "aviso";
    public const string Alerta = "alerta";
    public const string Bien = "bien";
}

public class AlertaModel
{
    public string Nivel { get; set; } = NivelAlerta.Info;
    public string Titulo { get; set; } = "";
    public string Detalle { get; set; } = "";
}

public class CategoriaCambioModel
{
    public string Nombre { get; set; } = "";
    public string Clasificacion { get; set; } = "";
    public decimal Actual { get; set; }
    public decimal Anterior { get; set; }
    public decimal Aumento => Actual - Anterior;
}

public class ImpactoCompraModel
{
    public decimal Monto { get; set; }
    public int Meses { get; set; }
    public decimal Mensualidad { get; set; }

    // Promedios de los últimos meses con movimientos (sin contar las compras a MSI, que se proyectan por mensualidad).
    public decimal IngresoPromedio { get; set; }
    public decimal GastoPromedio { get; set; }

    public decimal DineroPropioActual { get; set; }
    public decimal DineroPropioDespues { get; set; }
    public decimal? ColchonAntes { get; set; }
    public decimal? ColchonDespues { get; set; }

    public decimal LimiteCreditoDisponible { get; set; }
    public bool CabeEnCredito { get; set; }

    public string Veredicto { get; set; } = NivelAlerta.Info;
    public string Resumen { get; set; } = "";
    public List<ImpactoMesModel> Proyeccion { get; set; } = [];
}

public class ImpactoMesModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal MsiExistentes { get; set; }
    public decimal NuevaCompra { get; set; }

    // Ingreso promedio - gasto promedio - mensualidades MSI (las actuales y la nueva compra).
    public decimal FlujoLibre { get; set; }
}
