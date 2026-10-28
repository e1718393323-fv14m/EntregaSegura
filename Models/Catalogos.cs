namespace EntregaSegura.Models
{
    // Fila de uis.clientes
    public class Cliente
    {
        public long id { get; set; }
        public string nombre { get; set; } = "";
        public string? ruc { get; set; }
        public string direccion { get; set; } = "";
        public string? telefono { get; set; }
        public double latitud { get; set; }
        public double longitud { get; set; }
    }

    // Fila de uis.productos
    public class Producto
    {
        public long id { get; set; }
        public string codigo { get; set; } = "";
        public string descripcion { get; set; } = "";
        public decimal precio { get; set; }
    }

    // Fila de uis.detalle_orden (qué producto y cuántas unidades lleva la orden)
    public class DetalleOrden
    {
        public int cantidad { get; set; }
        public Producto producto { get; set; } = new Producto();

        public decimal Subtotal => cantidad * producto.precio;
        public string Texto => $"{cantidad} x {producto.descripcion}";
        public string SubtotalTexto => $"${Subtotal:F2}";
    }

    // Fila de uis.vehiculos
    public class Vehiculo
    {
        public string placa { get; set; } = "";
        public string modelo { get; set; } = "";
        public int capacidad_kg { get; set; }
    }

    // Fila de uis.roles
    public class Rol
    {
        public string nombre { get; set; } = "CONDUCTOR";
    }

    // Fila de uis.conductores (perfil del usuario que inició sesión)
    public class Conductor
    {
        public long id { get; set; }
        public string email { get; set; } = "";
        public string nombres { get; set; } = "";
        public string cedula { get; set; } = "";
        public string? telefono { get; set; }
        public Vehiculo? vehiculo { get; set; }
        public Rol? rol { get; set; }

        public bool EsSupervisor => rol?.nombre == "SUPERVISOR";
        public string RolTexto => EsSupervisor ? "Supervisor" : "Conductor";
    }

    // Fila de uis.novedades (la usa el supervisor para revisarlas)
    public class Novedad
    {
        public long id { get; set; }
        public string descripcion { get; set; } = "";
        public DateTime fecha { get; set; }
        public bool revisada { get; set; }
        public string? respuesta { get; set; }
        public string conductor_email { get; set; } = "";
        public MotivoNovedad motivo { get; set; } = new MotivoNovedad();
        public Orden orden { get; set; } = new Orden();

        public string Titulo => $"{orden.numero} - {orden.cliente.nombre}";
        public string Detalle => $"{motivo.descripcion}: {descripcion}";
        public string Info => $"{conductor_email} · {fecha.ToLocalTime():dd/MM HH:mm}";
        public string EstadoTexto => revisada ? "Revisada: " + (respuesta ?? "") : "Por revisar (toque para revisar)";
        public Color ColorEstado => revisada ? Color.FromArgb("#1E7B34") : Color.FromArgb("#B3261E");
    }

    // Fila de uis.motivos_novedad (se muestra en el Picker)
    public class MotivoNovedad
    {
        public long id { get; set; }
        public string descripcion { get; set; } = "";

        public override string ToString() => descripcion;
    }
}
