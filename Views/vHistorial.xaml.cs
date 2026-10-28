using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vHistorial : ContentPage
{
    public vHistorial()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarOrdenes();
    }

    private async Task CargarOrdenes()
    {
        try
        {
            refrescar.IsRefreshing = true;
            lstOrdenes.ItemsSource = await SupabaseService.ObtenerOrdenes("REALIZADAS");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudieron cargar el historial: " + ex.Message, "Cerrar");
        }
        finally
        {
            refrescar.IsRefreshing = false;
        }
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
            // En el historial solo se consulta: la orden ya no se puede modificar
            await Navigation.PushAsync(new vDetalleOrden(orden, soloLectura: true));
        }
    }
}
