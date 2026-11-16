using System.Text.Json.Serialization;

namespace SIGA.E2ETests.Common;

public class ApiResponseDto<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }
}

public class LoginRequestDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("usuario")]
    public UserInfoDto? Usuario { get; set; }
}

public class UserInfoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = new();
}

public class UserProfileDto
{
    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("personaId")]
    public int PersonaId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("documento")]
    public string? Documento { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("permisos")]
    public List<string> Permisos { get; set; } = new();

    [JsonPropertyName("ultimoAcceso")]
    public DateTime? UltimoAcceso { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public class RolDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("esSistema")]
    public bool EsSistema { get; set; }

    [JsonPropertyName("totalUsuarios")]
    public int TotalUsuarios { get; set; }

    [JsonPropertyName("permisos")]
    public List<string> Permisos { get; set; } = new();
}

public class CreateRolDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("permisos")]
    public List<string> Permisos { get; set; } = new();
}

public class UpdateRolDto
{
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("permisos")]
    public List<string> Permisos { get; set; } = new();
}

public class PermisoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("modulo")]
    public string Modulo { get; set; } = string.Empty;
}

public class EmpleadoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("personaId")]
    public int PersonaId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("nombreCompleto")]
    public string NombreCompleto { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("documento")]
    public string? Documento { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public class CreateEmpleadoDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("documento")]
    public string Documento { get; set; } = string.Empty;

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }
}

public class UpdateEmpleadoDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("documento")]
    public string Documento { get; set; } = string.Empty;

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("rolId")]
    public int RolId { get; set; }
}

public class ClienteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cuit")]
    public string? Cuit { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public class CreateClienteDto
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("cuit")]
    public string Cuit { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }
}

public class MovimientoComercialDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("numero")]
    public string Numero { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public int Tipo { get; set; }

    [JsonPropertyName("estado")]
    public int Estado { get; set; }

    [JsonPropertyName("clienteId")]
    public int? ClienteId { get; set; }

    [JsonPropertyName("clienteOrigenId")]
    public int? ClienteOrigenId { get; set; }

    [JsonPropertyName("clienteDestinoId")]
    public int? ClienteDestinoId { get; set; }

    [JsonPropertyName("temporadaId")]
    public int TemporadaId { get; set; }

    [JsonPropertyName("montoBruto")]
    public decimal MontoBruto { get; set; }

    [JsonPropertyName("porcentajeComision")]
    public decimal PorcentajeComision { get; set; }

    [JsonPropertyName("montoComision")]
    public decimal MontoComision { get; set; }

    [JsonPropertyName("montoNeto")]
    public decimal MontoNeto { get; set; }

    [JsonPropertyName("montoTotal")]
    public decimal MontoTotal { get; set; }

    [JsonPropertyName("usuarioRegistroId")]
    public int UsuarioRegistroId { get; set; }

    [JsonPropertyName("fechaRegistro")]
    public DateTime FechaRegistro { get; set; }

    [JsonPropertyName("detalles")]
    public List<DetalleMovimientoDto> Detalles { get; set; } = new();

    [JsonPropertyName("historialAprobaciones")]
    public List<RegistroAprobacionDto> HistorialAprobaciones { get; set; } = new();
}

public class DetalleMovimientoDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("claseBulbo")]
    public int ClaseBulbo { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("precioUnitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }
}

public class CreateMovimientoComercialDto
{
    [JsonPropertyName("tipo")]
    public int Tipo { get; set; }

    [JsonPropertyName("clienteId")]
    public int? ClienteId { get; set; }

    [JsonPropertyName("clienteOrigenId")]
    public int? ClienteOrigenId { get; set; }

    [JsonPropertyName("clienteDestinoId")]
    public int? ClienteDestinoId { get; set; }

    [JsonPropertyName("temporadaId")]
    public int TemporadaId { get; set; } = 1;

    [JsonPropertyName("porcentajeComision")]
    public decimal PorcentajeComision { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    [JsonPropertyName("detalles")]
    public List<CreateDetalleMovimientoDto> Detalles { get; set; } = new();
}

public class CreateDetalleMovimientoDto
{
    [JsonPropertyName("claseBulbo")]
    public int ClaseBulbo { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("precioUnitario")]
    public decimal PrecioUnitario { get; set; }
}

public class RechazarMovimientoDto
{
    [JsonPropertyName("motivo")]
    public string Motivo { get; set; } = string.Empty;
}

public class FacturarMovimientoDto
{
    [JsonPropertyName("numeroFactura")]
    public string? NumeroFactura { get; set; }
}

public class VerificarClienteNuevoDto
{
    [JsonPropertyName("esNuevo")]
    public bool EsNuevo { get; set; }

    [JsonPropertyName("requiereContrato")]
    public bool RequiereContrato { get; set; }
}

public class RegistroAprobacionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("usuarioId")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("fecha")]
    public DateTime Fecha { get; set; }

    [JsonPropertyName("accion")]
    public string Accion { get; set; } = string.Empty;

    [JsonPropertyName("notas")]
    public string? Notas { get; set; }
}

public class PrecioReferenciaDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("temporadaId")]
    public int TemporadaId { get; set; }

    [JsonPropertyName("claseBulbo")]
    public int ClaseBulbo { get; set; }

    [JsonPropertyName("nombreClase")]
    public string NombreClase { get; set; } = string.Empty;

    [JsonPropertyName("precioUnitario")]
    public decimal PrecioUnitario { get; set; }
}

public class UpsertPrecioReferenciaDto
{
    [JsonPropertyName("temporadaId")]
    public int TemporadaId { get; set; }

    [JsonPropertyName("claseBulbo")]
    public int ClaseBulbo { get; set; }

    [JsonPropertyName("precioUnitario")]
    public decimal PrecioUnitario { get; set; }
}

public class FilaDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("identificacion")]
    public string Identificacion { get; set; } = string.Empty;

    [JsonPropertyName("numeroFila")]
    public int? NumeroFila { get; set; }

    [JsonPropertyName("capacidad")]
    public int Capacidad { get; set; }

    [JsonPropertyName("totalLotes")]
    public int TotalLotes { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public class LoteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("orden")]
    public int Orden { get; set; }

    [JsonPropertyName("cantidadPlantadaTotal")]
    public int CantidadPlantadaTotal { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;
}

public class RegistrarDistribucionRequestDto
{
    [JsonPropertyName("sucursalId")]
    public int SucursalId { get; set; } = 1;

    [JsonPropertyName("temporadaId")]
    public int TemporadaId { get; set; } = 1;

    [JsonPropertyName("filaId")]
    public int? FilaId { get; set; }

    [JsonPropertyName("numeroFila")]
    public int? NumeroFila { get; set; }

    [JsonPropertyName("identificacionFila")]
    public string? IdentificacionFila { get; set; }

    [JsonPropertyName("capacidadFila")]
    public int CapacidadFila { get; set; } = 1000;

    [JsonPropertyName("numeroLote")]
    public int? NumeroLote { get; set; }

    [JsonPropertyName("clienteId")]
    public int ClienteId { get; set; }

    [JsonPropertyName("detalles")]
    public List<DetalleDistribucionDto> Detalles { get; set; } = new();
}

public class DetalleDistribucionDto
{
    [JsonPropertyName("clase")]
    public int Clase { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }
}

public class StockClienteDto
{
    [JsonPropertyName("clienteId")]
    public int ClienteId { get; set; }

    [JsonPropertyName("totalBulbos")]
    public int TotalBulbos { get; set; }

    [JsonPropertyName("stockPorClase")]
    public List<StockClaseDto> StockPorClase { get; set; } = new();
}

public class StockClaseDto
{
    [JsonPropertyName("clase")]
    public int Clase { get; set; }

    [JsonPropertyName("nombreClase")]
    public string NombreClase { get; set; } = string.Empty;

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }
}
