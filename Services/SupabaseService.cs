using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EntregaSegura.Models;

namespace EntregaSegura.Services
{

    public static class SupabaseService
    {
        // ===== DATOS DE CONEXIÓN =====

        private const string Url = "https://psxxnkjmmlqsbiyygxnj.supabase.co";
        private const string ApiKey = "sb_publishable_0O6Kzs6F0xY3hiFzMFVJlA_ExIGXmhF";
        private const string Esquema = "uis";
        private const string Bucket = "fotos-entregas";

        private static readonly HttpClient cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private static HttpRequestMessage CrearPeticion(HttpMethod metodo, string ruta)
        {
            var peticion = new HttpRequestMessage(metodo, Url + ruta);
            peticion.Headers.Add("apikey", ApiKey);
            if (Sesion.Token != "")
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Sesion.Token);

            peticion.Headers.Add("Accept-Profile", Esquema);
            peticion.Headers.Add("Content-Profile", Esquema);
            return peticion;
        }

        private static async Task RevisarRespuesta(HttpResponseMessage respuesta)
        {
            if (respuesta.IsSuccessStatusCode) return;
            string detalle = await respuesta.Content.ReadAsStringAsync();
            throw new Exception($"Error del servidor ({(int)respuesta.StatusCode}): {detalle}");
        }

        public static async Task<bool> IniciarSesion(string correo, string contrasena)
        {
            var peticion = new HttpRequestMessage(HttpMethod.Post, Url + "/auth/v1/token?grant_type=password");
            peticion.Headers.Add("apikey", ApiKey);
            peticion.Content = JsonContent.Create(new { email = correo, password = contrasena });

            var respuesta = await cliente.SendAsync(peticion);
            if (!respuesta.IsSuccessStatusCode)
                return false;  

            string json = await respuesta.Content.ReadAsStringAsync();
            using var documento = JsonDocument.Parse(json);
            Sesion.Token = documento.RootElement.GetProperty("access_token").GetString() ?? "";
            Sesion.UsuarioId = documento.RootElement.GetProperty("user").GetProperty("id").GetString() ?? "";
            Sesion.Correo = correo.ToLower();
            return true;
        }

        public static async Task<Conductor?> ObtenerPerfil()
        {
            var peticion = CrearPeticion(HttpMethod.Get,
                "/rest/v1/conductores?select=id,email,nombres,cedula,telefono,vehiculo:vehiculos(placa,modelo,capacidad_kg),rol:roles(nombre)" +
                "&email=eq." + Uri.EscapeDataString(Sesion.Correo));

            var respuesta = await cliente.SendAsync(peticion);
            await RevisarRespuesta(respuesta);
            var lista = await respuesta.Content.ReadFromJsonAsync<List<Conductor>>();
            return lista?.FirstOrDefault();   
        }

        public static async Task<List<Orden>> ObtenerOrdenes(string estado)
        {
            string filtro = estado switch
            {
                "PENDIENTE" => "&estado=eq.PENDIENTE",
                "REALIZADAS" => "&estado=neq.PENDIENTE",
                "ENTREGADA" => "&estado=eq.ENTREGADA",
                "NOVEDAD" => "&estado=eq.NOVEDAD",
                _ => ""
            };
            string campos = "id,numero,secuencia,estado,fecha," +
                            "cliente:clientes(id,nombre,ruc,direccion,telefono,latitud,longitud)," +
                            "conductor:conductores(nombres)," +
                            "detalle:detalle_orden(cantidad,producto:productos(id,codigo,descripcion,precio))";
            var peticion = CrearPeticion(HttpMethod.Get, $"/rest/v1/ordenes?select={campos}{filtro}&order=conductor_id.asc,secuencia.asc");

            var respuesta = await cliente.SendAsync(peticion);
            await RevisarRespuesta(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<List<Orden>>() ?? new List<Orden>();
        }

        // ===== 4. MOTIVOS DE NOVEDAD (catálogo para el Picker) =====
        public static async Task<List<MotivoNovedad>> ObtenerMotivos()
        {
            var peticion = CrearPeticion(HttpMethod.Get, "/rest/v1/motivos_novedad?select=id,descripcion&order=id.asc");
            var respuesta = await cliente.SendAsync(peticion);
            await RevisarRespuesta(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<List<MotivoNovedad>>() ?? new List<MotivoNovedad>();
        }

        public static async Task<List<Novedad>> ObtenerNovedades()
        {
            var peticion = CrearPeticion(HttpMethod.Get,
                "/rest/v1/novedades?select=id,descripcion,fecha,revisada,respuesta,conductor_email," +
                "motivo:motivos_novedad(id,descripcion),orden:ordenes(numero,cliente:clientes(nombre))&order=revisada.asc,fecha.desc");
            var respuesta = await cliente.SendAsync(peticion);
            await RevisarRespuesta(respuesta);
            return await respuesta.Content.ReadFromJsonAsync<List<Novedad>>() ?? new List<Novedad>();
        }

        public static async Task RevisarNovedad(long novedadId, string respuestaSupervisor)
        {
            var peticion = CrearPeticion(HttpMethod.Patch, $"/rest/v1/novedades?id=eq.{novedadId}");
            peticion.Content = JsonContent.Create(new { revisada = true, respuesta = respuestaSupervisor });
            await RevisarRespuesta(await cliente.SendAsync(peticion));
        }

        public static async Task<string> SubirArchivo(byte[] bytes, string nombreArchivo, string tipo)
        {
            string ruta = $"{Sesion.UsuarioId}/{nombreArchivo}";
            var peticion = CrearPeticion(HttpMethod.Post, $"/storage/v1/object/{Bucket}/{ruta}");
            peticion.Content = new ByteArrayContent(bytes);
            peticion.Content.Headers.ContentType = new MediaTypeHeaderValue(tipo);

            var respuesta = await cliente.SendAsync(peticion);
            await RevisarRespuesta(respuesta);
            return ruta;
        }

        public static async Task RegistrarEntrega(long ordenId, string recibe, double latitud, double longitud,
            string foto, string firma)
        {
            var datos = new { orden_id = ordenId, recibe, latitud, longitud, foto, firma };
            var peticion = CrearPeticion(HttpMethod.Post, "/rest/v1/entregas");
            peticion.Content = JsonContent.Create(datos);
            await RevisarRespuesta(await cliente.SendAsync(peticion));

            await CambiarEstado(ordenId, "ENTREGADA");
        }

        // ===== 7. REGISTRAR UNA NOVEDAD (cliente ausente, producto dañado, etc.) =====
        public static async Task RegistrarNovedad(long ordenId, long motivoId, string descripcion,
            double? latitud, double? longitud, string? foto)
        {
            var datos = new { orden_id = ordenId, motivo_id = motivoId, descripcion, latitud, longitud, foto };
            var peticion = CrearPeticion(HttpMethod.Post, "/rest/v1/novedades");
            peticion.Content = JsonContent.Create(datos);
            await RevisarRespuesta(await cliente.SendAsync(peticion));

            await CambiarEstado(ordenId, "NOVEDAD");
        }

        private static async Task CambiarEstado(long ordenId, string estado)
        {
            var peticion = CrearPeticion(HttpMethod.Patch, $"/rest/v1/ordenes?id=eq.{ordenId}");
            peticion.Content = JsonContent.Create(new { estado });
            await RevisarRespuesta(await cliente.SendAsync(peticion));
        }
    }
}
