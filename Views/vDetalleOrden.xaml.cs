using System.Globalization;
using EntregaSegura.Models;

namespace EntregaSegura.Views;

public partial class vDetalleOrden : ContentPage
{
    private readonly Orden orden;

    // Recibimos la orden seleccionada en la lista (paso de datos entre pantallas por el constructor)
    public vDetalleOrden(Orden ordenSeleccionada, bool soloLectura = false)
    {
        InitializeComponent();
        orden = ordenSeleccionada;

        lblNumero.Text = orden.numero;
        lblCliente.Text = orden.cliente.nombre;
        lblEstado.Text = "Estado: " + orden.EstadoTexto;
        lblEstado.TextColor = orden.ColorEstado;
        lblConductor.Text = orden.ConductorTexto;
        lblRuc.Text = "RUC: " + (orden.cliente.ruc ?? "-");
        lblDireccion.Text = orden.cliente.direccion;
        lblTelefono.Text = string.IsNullOrEmpty(orden.cliente.telefono) ? "Sin teléfono" : orden.cliente.telefono;

        // Lista de productos: una línea por producto con su subtotal
        lblProductos.Text = string.Join("\n", orden.detalle.Select(d => $"• {d.Texto}  ({d.SubtotalTexto})"));
        lblUnidades.Text = $"{orden.TotalUnidades} unidades";
        lblTotal.Text = $"Total ${orden.Total:F2}";

        // Si la orden ya se cerró, ocultamos los botones de entrega
        btnEntregar.IsVisible = !soloLectura && orden.estado == "PENDIENTE";
        btnNovedad.IsVisible = !soloLectura && orden.estado == "PENDIENTE";
        btnLlamar.IsEnabled = !string.IsNullOrEmpty(orden.cliente.telefono);
    }

    // Servicio web externo: abre Google Maps con la ruta hasta el cliente
    private async void btnComoLlegar_Clicked(object sender, EventArgs e)
    {
        try
        {
            string destino = orden.cliente.latitud.ToString(CultureInfo.InvariantCulture) + "," +
                             orden.cliente.longitud.ToString(CultureInfo.InvariantCulture);
            await Launcher.OpenAsync(new Uri("https://www.google.com/maps/dir/?api=1&destination=" + destino));
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo abrir Google Maps: " + ex.Message, "Cerrar");
        }
    }

    private async void btnLlamar_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(orden.cliente.telefono!);
            else
                await DisplayAlert("Llamar", "Este dispositivo no puede hacer llamadas. Teléfono: " + orden.cliente.telefono, "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", ex.Message, "Cerrar");
        }
    }

    private async void btnEntregar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vConfirmarEntrega(orden));
    }

    private async void btnNovedad_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vNovedad(orden));
    }
}
