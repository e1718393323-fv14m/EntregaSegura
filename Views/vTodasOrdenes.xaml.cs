using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

// SUPERVISOR: órdenes de todos los conductores con filtro por estado
public partial class vTodasOrdenes : ContentPage
{
    public vTodasOrdenes()
    {
        InitializeComponent();
        pkFiltro.SelectedIndex = 0;   // "Todas" (esto también carga la lista)
    }

    private async Task CargarOrdenes()
    {
        try
        {
            refrescar.IsRefreshing = true;
            string estado = pkFiltro.SelectedIndex switch
            {
                1 => "PENDIENTE",
                2 => "ENTREGADA",
                3 => "NOVEDAD",
                _ => "TODAS"
            };
            var ordenes = await SupabaseService.ObtenerOrdenes(estado);
            lstOrdenes.ItemsSource = ordenes;
            lblCantidad.Text = $"{ordenes.Count} orden(es)";
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudieron cargar las órdenes: " + ex.Message, "Cerrar");
        }
        finally
        {
            refrescar.IsRefreshing = false;
        }
    }

    private async void pkFiltro_SelectedIndexChanged(object sender, EventArgs e)
    {
        await CargarOrdenes();
    }

    private async void refrescar_Refreshing(object sender, EventArgs e)
    {
        await CargarOrdenes();
    }

    private async void lstOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Orden orden)
        {
            lstOrdenes.SelectedItem = null;
            // El supervisor solo consulta: no puede registrar entregas
            await Navigation.PushAsync(new vDetalleOrden(orden, soloLectura: true));
        }
    }
}
