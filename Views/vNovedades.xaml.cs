using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

// SUPERVISOR: lista de novedades de todos los conductores; puede marcarlas como revisadas
public partial class vNovedades : ContentPage
{
    public vNovedades()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarNovedades();
    }

    private async Task CargarNovedades()
    {
        try
        {
            refrescar.IsRefreshing = true;
            lstNovedades.ItemsSource = await SupabaseService.ObtenerNovedades();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudieron cargar las novedades: " + ex.Message, "Cerrar");
        }
        finally
        {
            refrescar.IsRefreshing = false;
        }
    }

    private async void refrescar_Refreshing(object sender, EventArgs e)
    {
        await CargarNovedades();
    }

    private async void lstNovedades_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Novedad novedad) return;
        lstNovedades.SelectedItem = null;

        if (novedad.revisada)
        {
            await DisplayAlert("Novedad revisada", $"{novedad.Titulo}\n{novedad.Detalle}\n\nRespuesta: {novedad.respuesta}", "Aceptar");
            return;
        }

        try
        {
            // Pedimos al supervisor un comentario para cerrar la novedad
            string? respuesta = await DisplayPromptAsync("Revisar novedad",
                $"{novedad.Titulo}\n{novedad.Detalle}\n\nEscriba la acción tomada:",
                "Guardar", "Cancelar", "Ej.: se reprograma para mañana", maxLength: 200);

            if (string.IsNullOrWhiteSpace(respuesta)) return;

            await SupabaseService.RevisarNovedad(novedad.id, respuesta.Trim());
            await DisplayAlert("Listo", "Novedad marcada como revisada", "Aceptar");
            await CargarNovedades();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo guardar la revisión: " + ex.Message, "Cerrar");
        }
    }
}
