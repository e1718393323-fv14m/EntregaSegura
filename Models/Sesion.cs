namespace EntregaSegura.Models
{
    // Guarda los datos del conductor que inició sesión mientras la app está abierta.
    public static class Sesion
    {
        public static string Token { get; set; } = "";
        public static string UsuarioId { get; set; } = "";
        public static string Correo { get; set; } = "";

        // Perfil del conductor (tabla uis.conductores); se carga después del login
        public static Conductor? Conductor { get; set; }

        // Nombre para el saludo: el del perfil, o la parte del correo antes de la @
        public static string Nombre => Conductor?.nombres ?? (Correo.Contains('@') ? Correo.Split('@')[0] : Correo);

        public static void Cerrar()
        {
            Token = "";
            UsuarioId = "";
            Correo = "";
            Conductor = null;
        }
    }
}
