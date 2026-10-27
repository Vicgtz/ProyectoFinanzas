using Finanzas.Models.Dashboard;

namespace Finanzas.Models.Presupuesto;

// Datos de entrada para crear o cambiar un presupuesto mensual de una categoría.
public class PresupuestoModel
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }
    public decimal Monto { get; set; }
}

public class PresupuestoItemModel
{
    public int Id { get; set; }
    public int CategoriaId { get; set; }

    // Con emoji cuando la categoría lo tiene.
    public string Categoria { get; set; } = "";
    public decimal Monto { get; set; }
    public decimal Gastado { get; set; }

    public decimal Disponible => Monto - Gastado;
    public decimal Porcentaje => Monto <= 0 ? 0 : Gastado / Monto * 100;

    // Bueno: menos de 80 %. Regular: de 80 % a menos de 100 %. Alerta: 100 % o más.
    public string Nivel => Porcentaje >= 100 ? NivelIndicador.Alerta : Porcentaje >= 80 ? NivelIndicador.Regular : NivelIndicador.Bueno;
}

public class PresupuestoResumenModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public List<PresupuestoItemModel> Items { get; set; } = [];

    public decimal TotalPresupuestado { get; set; }

    // Gastado en las categorías que tienen presupuesto.
    public decimal TotalGastado { get; set; }

    // Gastos del mes en categorías sin presupuesto (y sin categoría).
    public decimal GastoSinPresupuesto { get; set; }

    public decimal Disponible => TotalPresupuestado - TotalGastado;
}
