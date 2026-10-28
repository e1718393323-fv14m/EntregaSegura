namespace EntregaSegura.Models
{
    // Fila de uis.ordenes. Supabase nos devuelve también el cliente y el detalle
    // (productos) en la misma consulta, por eso tiene objetos dentro.
    public class Orden
    {
        public long id { get; set; }
        public string numero { get; set; } = "";
        public int secuencia { get; set; }
        public string estado { get; set; } = "PENDIENTE";
        public DateTime fecha { get; set; }

        public Cliente cliente { get; set; } = new Cliente();
        public Conductor? conductor { get; set; }   // lo usa el supervisor para saber quién entrega
        public List<DetalleOrden> detalle { get; set; } = new List<DetalleOrden>();

        // ===== Propiedades calculadas para mostrar en pantalla =====
        public string Titulo => $"{secuencia}. {numero} - {cliente.nombre}";

        public string ConductorTexto => "Conductor: " + (conductor?.nombres ?? "-");

        public int TotalUnidades => detalle.Sum(d => d.cantidad);

        public decimal Total => detalle.Sum(d => d.Subtotal);

        public string Resumen => $"{detalle.Count} producto(s) · {TotalUnidades} unidades · ${Total:F2}";

        public string EstadoTexto => estado switch
        {
            "ENTREGADA" => "Entregada",
            "NOVEDAD" => "Con novedad",
            _ => "Pendiente"
        };

        public Color ColorEstado => estado switch
        {
            "ENTREGADA" => Color.FromArgb("#1E7B34"),
            "NOVEDAD" => Color.FromArgb("#B3261E"),
            _ => Color.FromArgb("#8A4B00")
        };
    }
}
