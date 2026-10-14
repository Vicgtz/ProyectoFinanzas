namespace Finanzas.Models.Msi;

public class MsiResumenModel
{
    // Lo que aún falta por cobrar de todas las compras a meses sin intereses.
    public decimal TotalPendiente { get; set; }

    // Mensualidades que se cobrarán en el mes en curso (las que aún no han pasado) y en el mes siguiente.
    public decimal MesActual { get; set; }
    public decimal MesSiguiente { get; set; }

    public List<MsiMesModel> Calendario { get; set; } = [];
    public List<MsiPlanModel> Planes { get; set; } = [];
}

public class MsiMesModel
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Importe { get; set; }
}

public class MsiPlanModel
{
    public long OperacionId { get; set; }
    public string Descripcion { get; set; } = "";
    public string CuentaNombre { get; set; } = "";
    public DateTime FechaCompra { get; set; }
    public decimal MontoCompra { get; set; }
    public int NumeroMeses { get; set; }
    public decimal ImporteMensual { get; set; }
    public int MensualidadesTranscurridas { get; set; }
    public int MensualidadesRestantes { get; set; }
    public decimal SaldoPendiente { get; set; }
    public DateTime? ProximaMensualidad { get; set; }
    public DateTime UltimaMensualidad { get; set; }
}
