using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vPrincipal : ContentPage
{
    public vPrincipal()
    {
        InitializeComponent();
        lblSaludo.Text = "Hola, " + Sesion.Nombre;
        lblFecha.Text = DateTime.Now.ToString("dddd dd 'de' MMMM yyyy");

        var vehiculo = Sesion.Conductor?.vehiculo;
        lblVehiculo.Text = vehiculo == null ? "Sin vehículo asignado" : $"Vehículo: {vehiculo.modelo} ({vehiculo.placa})";
    }

    // Cada vez que se muestra la pantalla actualizamos el resumen
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var pendientes = await SupabaseService.ObtenerOrdenes("PENDIENTE");
            var realizadas = await SupabaseService.ObtenerOrdenes("REALIZADAS");
            lblPendientes.Text = "Pendientes: " + pendientes.Count;
            lblRealizadas.Text = "Realizadas: " + realizadas.Count;
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo cargar el resumen: " + ex.Message, "Cerrar");
        }
    }

    private async void btnEntregas_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vEntregas());
    }

    private async void btnHistorial_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vHistorial());
    }

    private async void btnPerfil_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vPerfil());
    }

    private async void btnSalir_Clicked(object sender, EventArgs e)
    {
        bool salir = await DisplayAlert("Cerrar sesión", "¿Desea salir de la aplicación?", "Sí", "No");
        if (salir)
        {
            Sesion.Cerrar();
            await Navigation.PopToRootAsync();   // regresa al login
        }
    }
}
