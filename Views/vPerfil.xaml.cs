using EntregaSegura.Models;
using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vPerfil : ContentPage
{
    public vPerfil()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            indicador.IsRunning = true;

            // Volvemos a consultar el perfil por si cambió en la base de datos
            Sesion.Conductor = await SupabaseService.ObtenerPerfil();
            var conductor = Sesion.Conductor;
            if (conductor == null)
            {
                await DisplayAlert("Aviso", "No se encontró su perfil de conductor", "Cerrar");
                return;
            }

            lblNombre.Text = conductor.nombres;
            lblCorreo.Text = conductor.email;
            lblRol.Text = "Rol: " + conductor.RolTexto;
            lblCedula.Text = "Cédula: " + conductor.cedula;
            lblTelefono.Text = "Teléfono: " + (conductor.telefono ?? "-");

            if (conductor.vehiculo != null)
            {
                lblPlaca.Text = "Placa: " + conductor.vehiculo.placa;
                lblModelo.Text = "Modelo: " + conductor.vehiculo.modelo;
                lblCapacidad.Text = $"Capacidad: {conductor.vehiculo.capacidad_kg} kg";
            }
            else
            {
                lblPlaca.Text = "Sin vehículo asignado";
            }

            // Resumen calculado con las órdenes ya atendidas
            var realizadas = await SupabaseService.ObtenerOrdenes("REALIZADAS");
            var entregadas = realizadas.Where(o => o.estado == "ENTREGADA").ToList();
            string quien = conductor.EsSupervisor ? "del equipo" : "realizadas";
            lblEntregadas.Text = $"Entregas {quien}: {entregadas.Count}";
            lblNovedades.Text = $"Órdenes con novedad: {realizadas.Count(o => o.estado == "NOVEDAD")}";
            lblMonto.Text = $"Valor entregado: ${entregadas.Sum(o => o.Total):F2}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo cargar el perfil: " + ex.Message, "Cerrar");
        }
        finally
        {
            indicador.IsRunning = false;
        }
    }
}
