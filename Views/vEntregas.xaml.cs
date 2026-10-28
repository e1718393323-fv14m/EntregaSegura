using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vEntregas : ContentPage
{
    public vEntregas()
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
            lstOrdenes.ItemsSource = await SupabaseService.ObtenerOrdenes("PENDIENTE");
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

    private async void refrescar_Refreshing(object sender, EventArgs e)
    {
        await CargarOrdenes();
    }

    private async void lstOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Orden orden)
        {
            lstOrdenes.SelectedItem = null;   // para poder volver a tocar la misma orden
            await Navigation.PushAsync(new vDetalleOrden(orden));
        }
    }
}
