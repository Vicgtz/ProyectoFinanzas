namespace Finanzas.Models.Persona;

public class PersonaModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activa { get; set; } = true;

    // Saldos pendientes con la persona; los calcula la API y se ignoran al guardar.
    public decimal PorCobrar { get; set; }
    public decimal PorPagar { get; set; }

    // Positivo: la persona te debe; negativo: tú le debes.
    public decimal Neto => PorCobrar - PorPagar;
}
