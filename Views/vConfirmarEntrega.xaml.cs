using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vConfirmarEntrega : ContentPage
{
    private readonly Orden orden;
    private byte[]? foto;          // bytes de la foto tomada
    private Location? ubicacion;   // coordenadas del GPS

    public vConfirmarEntrega(Orden ordenSeleccionada)
    {
        InitializeComponent();
        orden = ordenSeleccionada;
        lblOrden.Text = $"{orden.numero} - {orden.cliente.nombre} ({orden.TotalUnidades} unidades)";
        ActualizarChecklist();
    }

    // ===== Lista de verificación dinámica: se actualiza con cada dato que se completa =====
    private void ActualizarChecklist()
    {
        bool nombre = (txtRecibe.Text?.Trim().Length ?? 0) >= 3;
        bool tieneFoto = foto != null;
        bool tieneFirma = drwFirma.Lines.Count > 0;
        bool tieneGps = ubicacion != null;
        bool conforme = chkConforme.IsChecked;

        lblChecklist.Text =
            $"{Marca(nombre)} Nombre de quien recibe\n" +
            $"{Marca(tieneFoto)} Foto de la entrega\n" +
            $"{Marca(tieneFirma)} Firma del cliente\n" +
            $"{Marca(tieneGps)} Ubicación GPS\n" +
            $"{Marca(conforme)} Cliente conforme";

        // El botón Guardar solo se habilita cuando todo está completo
        btnGuardar.IsEnabled = nombre && tieneFoto && tieneFirma && tieneGps && conforme;
    }

    private static string Marca(bool listo) => listo ? "✔" : "✘";

    private void txtRecibe_TextChanged(object sender, TextChangedEventArgs e) => ActualizarChecklist();

    private void chkConforme_CheckedChanged(object sender, CheckedChangedEventArgs e) => ActualizarChecklist();

    private void drwFirma_DrawingLineCompleted(object sender, CommunityToolkit.Maui.Core.DrawingLineCompletedEventArgs e) => ActualizarChecklist();

    // Al abrir la pantalla leemos el GPS automáticamente
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (ubicacion == null) await LeerUbicacion();
    }

    // ===== Sensor 1: CÁMARA =====
    private async void btnFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            FileResult? archivo;
            if (MediaPicker.Default.IsCaptureSupported)
                archivo = await MediaPicker.Default.CapturePhotoAsync();
            else
                archivo = await MediaPicker.Default.PickPhotoAsync();   // en PC sin cámara se elige una imagen

            if (archivo == null) return;   // el usuario canceló

            using Stream stream = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await stream.CopyToAsync(memoria);
            foto = memoria.ToArray();

            imgFoto.Source = ImageSource.FromStream(() => new MemoryStream(foto));
            lblSinFoto.IsVisible = false;
            btnFoto.Text = "Repetir foto";
            ActualizarChecklist();
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permiso", "Debe permitir el uso de la cámara", "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo tomar la foto: " + ex.Message, "Cerrar");
        }
    }

    // ===== Sensor 2: GPS =====
    private async void btnUbicacion_Clicked(object sender, EventArgs e)
    {
        await LeerUbicacion();
    }

    private async Task LeerUbicacion()
    {
        try
        {
            lblUbicacion.Text = "Leyendo ubicación...";
            var solicitud = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            ubicacion = await Geolocation.Default.GetLocationAsync(solicitud);

            if (ubicacion != null)
                lblUbicacion.Text = $"Ubicación: {ubicacion.Latitude:F5}, {ubicacion.Longitude:F5}";
            else
                lblUbicacion.Text = "No se obtuvo la ubicación";
            ActualizarChecklist();
        }
        catch (FeatureNotEnabledException)
        {
            lblUbicacion.Text = "Ubicación: GPS apagado";
            await DisplayAlert("GPS", "Encienda el GPS del teléfono", "Aceptar");
        }
        catch (PermissionException)
        {
            lblUbicacion.Text = "Ubicación: sin permiso";
            await DisplayAlert("Permiso", "Debe permitir el acceso a la ubicación", "Aceptar");
        }
        catch (Exception ex)
        {
            lblUbicacion.Text = "Ubicación: error";
            await DisplayAlert("ERROR", ex.Message, "Cerrar");
        }
    }

    private void btnBorrarFirma_Clicked(object sender, EventArgs e)
    {
        drwFirma.Clear();
        ActualizarChecklist();
    }

    // ===== GUARDAR =====
    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        // Validaciones antes de guardar
        string recibe = txtRecibe.Text?.Trim() ?? "";
        if (recibe.Length < 3)
        {
            await DisplayAlert("Aviso", "Ingrese el nombre de quien recibe", "Aceptar");
            return;
        }
        if (foto == null)
        {
            await DisplayAlert("Aviso", "Tome la foto de la entrega", "Aceptar");
            return;
        }
        if (drwFirma.Lines.Count == 0)
        {
            await DisplayAlert("Aviso", "Falta la firma del cliente", "Aceptar");
            return;
        }
        if (ubicacion == null)
        {
            await DisplayAlert("Aviso", "Lea la ubicación GPS antes de guardar", "Aceptar");
            return;
        }
        if (!chkConforme.IsChecked)
        {
            await DisplayAlert("Aviso", "Confirme que el cliente recibe conforme", "Aceptar");
            return;
        }

        try
        {
            btnGuardar.IsEnabled = false;
            indicador.IsRunning = true;

            // Convertimos la firma en una imagen PNG
            using Stream streamFirma = await drwFirma.GetImageStream(600, 250);
            using var memoria = new MemoryStream();
            await streamFirma.CopyToAsync(memoria);

            // Subimos la foto y la firma a Supabase Storage
            string fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string rutaFoto = await SupabaseService.SubirArchivo(foto, $"{orden.numero}_foto_{fecha}.jpg", "image/jpeg");
            string rutaFirma = await SupabaseService.SubirArchivo(memoria.ToArray(), $"{orden.numero}_firma_{fecha}.png", "image/png");

            // Guardamos la entrega en la base de datos
            await SupabaseService.RegistrarEntrega(orden.id, recibe, ubicacion.Latitude, ubicacion.Longitude, rutaFoto, rutaFirma);

            await DisplayAlert("Entrega registrada",
                $"Orden: {orden.numero}\nCliente: {orden.cliente.nombre}\nProductos: {orden.TotalUnidades} unidades\nTotal: ${orden.Total:F2}\nRecibe: {recibe}\nUbicación: {ubicacion.Latitude:F5}, {ubicacion.Longitude:F5}",
                "Aceptar");

            // Regresamos a la lista de entregas (cerramos esta pantalla y la del detalle)
            Navigation.RemovePage(Navigation.NavigationStack[^2]);
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo guardar la entrega: " + ex.Message, "Cerrar");
        }
        finally
        {
            indicador.IsRunning = false;
            ActualizarChecklist();
        }
    }
}
