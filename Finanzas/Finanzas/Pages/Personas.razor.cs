using Finanzas.Models.Persona;
using Finanzas.Services;
using Microsoft.AspNetCore.Components;

namespace Finanzas.Pages;

public partial class Personas
{
    [Inject] private PersonaService PersonaApi { get; set; } = default!;

    private List<PersonaModel> personas = [];
    private bool cargando = true;
    private string? error;

    private string busqueda = "";
    private bool mostrarInactivas;

    private bool modalAbierto;
    private bool editando;
    private bool guardando;
    private string? errorForm;
    private PersonaModel form = new();

    private List<PersonaModel> PersonasVisibles =>
        personas
            .Where(p => mostrarInactivas || p.Activa)
            .Where(p => string.IsNullOrWhiteSpace(busqueda) || p.Nombre.Contains(busqueda.Trim(), StringComparison.CurrentCultureIgnoreCase))
            .ToList();

    protected override async Task OnInitializedAsync() => await Cargar();

    private async Task Cargar()
    {
        cargando = true;
        error = null;
        var respuesta = await PersonaApi.GetList();
        if (respuesta.Success)
            personas = respuesta.Data ?? [];
        else
            error = respuesta.Message;
        cargando = false;
    }

    private void Nueva()
    {
        editando = false;
        errorForm = null;
        form = new PersonaModel { Activa = true };
        modalAbierto = true;
    }

    private void Editar(PersonaModel persona)
    {
        editando = true;
        errorForm = null;
        form = new PersonaModel { Id = persona.Id, Nombre = persona.Nombre, Activa = persona.Activa };
        modalAbierto = true;
    }

    private void CerrarModal()
    {
        if (!guardando) modalAbierto = false;
    }

    private async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(form.Nombre))
        {
            errorForm = "Ingresa el nombre de la persona";
            return;
        }

        errorForm = null;
        guardando = true;
        var respuesta = editando ? await PersonaApi.Edit(form) : await PersonaApi.Save(form);
        guardando = false;

        if (!respuesta.Success)
        {
            errorForm = respuesta.Message;
            return;
        }

        modalAbierto = false;
        await Cargar();
    }

    private async Task CambiarEstado(PersonaModel persona)
    {
        error = null;
        var respuesta = await PersonaApi.Edit(new PersonaModel { Id = persona.Id, Nombre = persona.Nombre, Activa = !persona.Activa });
        if (!respuesta.Success)
        {
            error = respuesta.Message;
            return;
        }
        await Cargar();
    }

    private static string Moneda(decimal valor) => $"${valor:N2}";
}
