namespace Finanzas.Models.Categoria;

public class CategoriaModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Emoji { get; set; }
    public string TipoSugerido { get; set; } = "";
    public string ClasificacionGasto { get; set; } = "";
    public bool Activa { get; set; }
    public int Orden { get; set; }

    // Solo la llena el servidor: true si la categoría es del usuario (editable); false si es general.
    public bool EsPropia { get; set; }

    public string NombreCompleto => string.IsNullOrEmpty(Emoji) ? Nombre : $"{Emoji} {Nombre}";
}
