using EntregaSegura.Services;

namespace EntregaSegura.Views;

public partial class vLogin : ContentPage
{
    public vLogin()
    {
        InitializeComponent();

        // Si el usuario marcó "Recordar", mostramos su correo guardado en el teléfono
        txtCorreo.Text = Preferences.Get("correo", "");
        chkRecordar.IsChecked = txtCorreo.Text != "";
    }

    // Muestra u oculta la contraseña al marcar la casilla
    private void chkMostrar_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        txtContrasena.IsPassword = !e.Value;
    }

    private async void btnIngresar_Clicked(object sender, EventArgs e)
    {
        try
        {
            string correo = txtCorreo.Text?.Trim() ?? "";
            string contrasena = txtContrasena.Text ?? "";

            // Validaciones de campos vacíos
            if (correo == "" || contrasena == "")
            {
                await DisplayAlert("Aviso", "Ingrese su correo y su contraseña", "Aceptar");
                return;
            }
            if (!correo.Contains('@'))
            {
                await DisplayAlert("Aviso", "Ingrese un correo válido", "Aceptar");
                return;
            }

            btnIngresar.IsEnabled = false;
            indicador.IsRunning = true;

            bool correcto = await SupabaseService.IniciarSesion(correo, contrasena);

            if (correcto)
            {
                // Buscamos el perfil del conductor en la tabla uis.conductores
                Models.Sesion.Conductor = await SupabaseService.ObtenerPerfil();
                if (Models.Sesion.Conductor == null)
                {
                    await DisplayAlert("Aviso", "Su usuario no está registrado en la tabla conductores", "Cerrar");
                    Models.Sesion.Cerrar();
                    return;
                }

                // Guardamos el correo en el teléfono si el usuario lo pidió
                Preferences.Set("correo", chkRecordar.IsChecked ? correo : "");
                txtContrasena.Text = "";

                // Según el rol abrimos el menú del conductor o el del supervisor
                if (Models.Sesion.Conductor.EsSupervisor)
                    await Navigation.PushAsync(new vSupervisor());
                else
                    await Navigation.PushAsync(new vPrincipal());
            }
            else
            {
                await DisplayAlert("ERROR", "Usuario/Contraseña incorrectos", "Cerrar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", "No se pudo conectar: " + ex.Message, "Cerrar");
        }
        finally
        {
            btnIngresar.IsEnabled = true;
            indicador.IsRunning = false;
        }
    }
}
