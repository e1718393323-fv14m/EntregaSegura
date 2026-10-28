using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

// Menú del SUPERVISOR: ve el avance de todos los conductores y revisa las novedades
public partial class vSupervisor : ContentPage
{
    public vSupervisor()
    {
        InitializeComponent();
        lblSaludo.Text = "Hola, " + Sesion.Nombre;
        lblFecha.Text = DateTime.Now.ToString("dddd dd 'de' MMMM yyyy");
    }

    // Cada vez que vuelve a esta pantalla se actualiza el resumen
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var ordenes = await SupabaseService.ObtenerOrdenes("TODAS");
            var novedades = await SupabaseService.ObtenerNovedades();

            int total = ordenes.Count;
            int pendientes = ordenes.Count(o => o.estado == "PENDIENTE");
            int entregadas = ordenes.Count(o => o.estado == "ENTREGADA");
            int conNovedad = ordenes.Count(o => o.estado == "NOVEDAD");
            int porRevisar = novedades.Count(n => !n.revisada);

            lblTotal.Text = "Órdenes: " + total;
            lblPendientes.Text = "Pendientes: " + pendientes;
            lblEntregadas.Text = "Entregadas: " + entregadas;
            lblNovedades.Text = "Con novedad: " + conNovedad;
            lblPorRevisar.Text = "Novedades por revisar: " + porRevisar;
            lblPorRevisar.TextColor = porRevisar > 0 ? Colors.Red : Colors.Green;

            // Avance = órdenes atendidas / total
            double avance = total == 0 ? 0 : (double)(entregadas + conNovedad) / total;
            await barAvance.ProgressTo(avance, 500, Easing.Linear);
            lblAvance.Text = $"Avance: {avance * 100:F0} %";
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo cargar el resumen: " + ex.Message, "Cerrar");
        }
    }

    private async void btnOrdenes_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vTodasOrdenes());
    }

    private async void btnNovedades_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vNovedades());
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
            await Navigation.PopToRootAsync();
        }
    }
}
