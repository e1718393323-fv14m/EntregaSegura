using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vNovedad : ContentPage
{
    private readonly Orden orden;
    private byte[]? foto;

    public vNovedad(Orden ordenSeleccionada)
    {
        InitializeComponent();
        orden = ordenSeleccionada;
        lblOrden.Text = $"{orden.numero} - {orden.cliente.nombre}";
    }

    // Los motivos vienen de la tabla uis.motivos_novedad
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (pkMotivo.ItemsSource != null) return;
        try
        {
            pkMotivo.ItemsSource = await SupabaseService.ObtenerMotivos();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudieron cargar los motivos: " + ex.Message, "Cerrar");
        }
    }

    // Contador de caracteres mientras se escribe
    private void txtDescripcion_TextChanged(object sender, TextChangedEventArgs e)
    {
        int largo = e.NewTextValue?.Length ?? 0;
        lblContador.Text = $"{largo}/300";
        lblContador.TextColor = largo < 5 ? Colors.Red : Colors.Green;
    }

    // Al elegir el motivo sugerimos un texto para ayudar al conductor
    private void pkMotivo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (pkMotivo.SelectedItem is MotivoNovedad motivo && string.IsNullOrWhiteSpace(txtDescripcion.Text))
            txtDescripcion.Placeholder = $"Explique: {motivo.descripcion.ToLower()}...";
    }

    private async void btnFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            FileResult? archivo = MediaPicker.Default.IsCaptureSupported
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();
            if (archivo == null) return;

            using Stream stream = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await stream.CopyToAsync(memoria);
            foto = memoria.ToArray();

            imgFoto.Source = ImageSource.FromStream(() => new MemoryStream(foto));
            lblSinFoto.IsVisible = false;
            btnFoto.Text = "Repetir foto";
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo tomar la foto: " + ex.Message, "Cerrar");
        }
    }

    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        if (pkMotivo.SelectedItem is not MotivoNovedad motivo)
        {
            await DisplayAlert("Aviso", "Seleccione el motivo de la novedad", "Aceptar");
            return;
        }
        string descripcion = txtDescripcion.Text?.Trim() ?? "";
        if (descripcion.Length < 5)
        {
            await DisplayAlert("Aviso", "Escriba una descripción (mínimo 5 letras)", "Aceptar");
            return;
        }

        try
        {
            btnGuardar.IsEnabled = false;
            indicador.IsRunning = true;

            // Intentamos leer el GPS; si no se puede, la novedad se guarda igual
            Location? ubicacion = null;
            try
            {
                ubicacion = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            }
            catch (Exception)
            {
                ubicacion = null;
            }

            string? rutaFoto = null;
            if (foto != null)
                rutaFoto = await SupabaseService.SubirArchivo(foto, $"{orden.numero}_novedad_{DateTime.Now:yyyyMMdd_HHmmss}.jpg", "image/jpeg");

            await SupabaseService.RegistrarNovedad(orden.id, motivo.id, descripcion, ubicacion?.Latitude, ubicacion?.Longitude, rutaFoto);

            await DisplayAlert("Novedad registrada", $"Orden: {orden.numero}\nMotivo: {motivo.descripcion}", "Aceptar");

            // Volvemos a la lista de entregas
            Navigation.RemovePage(Navigation.NavigationStack[^2]);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo guardar la novedad: " + ex.Message, "Cerrar");
        }
        finally
        {
            btnGuardar.IsEnabled = true;
            indicador.IsRunning = false;
        }
    }
}
