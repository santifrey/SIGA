using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Auth;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionUsuariosTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IPersonaRepository> _personaRepoMock;
    private readonly Mock<IRolRepository> _rolRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionUsuarios _service;

    public GestionUsuariosTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _personaRepoMock = new Mock<IPersonaRepository>();
        _rolRepoMock = new Mock<IRolRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionUsuarios(
            _usuarioRepoMock.Object,
            _personaRepoMock.Object,
            _rolRepoMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 1,
            PersonaId = 1,
            Username = "admin",
            PasswordHash = "hashedPassword",
            Estado = EstadoUsuario.Activo,
            RolId = 1,
            Rol = new Rol
            {
                Id = 1,
                Nombre = "ADMINISTRADOR",
                RolPermisos = new List<RolPermiso>()
            },
            Persona = new Persona { Id = 1, Nombre = "Admin", Apellido = "SIGA", Email = "admin@siga.com" }
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameWithRolesAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock.Setup(h => h.VerifyPassword("Admin123!", "hashedPassword"))
            .Returns(true);

        _tokenServiceMock.Setup(t => t.GenerateToken(usuario, "ADMINISTRADOR", It.IsAny<IEnumerable<string>>()))
            .Returns("fake-jwt-token-string");

        var request = new LoginRequestDto
        {
            Username = "admin",
            Password = "Admin123!"
        };

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("fake-jwt-token-string", result.Data.Token);
        Assert.Equal("admin", result.Data.Username);
        Assert.Equal("ADMINISTRADOR", result.Data.Rol);
        Assert.Contains("ADMINISTRADOR", result.Data.Roles);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "hashedPassword",
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameWithRolesAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock.Setup(h => h.VerifyPassword("WrongPassword", "hashedPassword"))
            .Returns(false);

        var request = new LoginRequestDto
        {
            Username = "admin",
            Password = "WrongPassword"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 2,
            Username = "inactivo",
            PasswordHash = "hashedPassword",
            Estado = EstadoUsuario.Inactivo
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameWithRolesAsync("inactivo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var request = new LoginRequestDto
        {
            Username = "inactivo",
            Password = "Password123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(request));
    }

    [Fact]
    public async Task GetMeAsync_WhenUserExists_ShouldReturnCompleteProfileWithPermissions()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var rol = new Rol
        {
            Id = 1,
            Nombre = "ADMINISTRADOR",
            EsSistema = true
        };
        var perm1 = new Permiso { Id = 1, Codigo = "ROLES_VER", Nombre = "Ver Roles", Modulo = "Seguridad" };
        var perm2 = new Permiso { Id = 2, Codigo = "ROLES_GESTIONAR", Nombre = "Gestionar Roles", Modulo = "Seguridad" };
        rol.RolPermisos.Add(new RolPermiso { RolId = 1, Rol = rol, PermisoId = 1, Permiso = perm1 });
        rol.RolPermisos.Add(new RolPermiso { RolId = 1, Rol = rol, PermisoId = 2, Permiso = perm2 });

        var usuario = new Usuario
        {
            Id = 1,
            PersonaId = 10,
            Username = "admin",
            RolId = 1,
            Rol = rol,
            Estado = EstadoUsuario.Activo,
            UltimoAcceso = now,
            Persona = new Persona
            {
                Id = 10,
                Nombre = "Administrador",
                Apellido = "SIGA",
                Documento = "12345678",
                Telefono = "3511112233",
                Email = "admin@azafran.com"
            }
        };

        _usuarioRepoMock.Setup(r => r.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        // Act
        var result = await _service.GetMeAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(1, data.UsuarioId);
        Assert.Equal(10, data.PersonaId);
        Assert.Equal("admin", data.Username);
        Assert.Equal("Administrador", data.Nombre);
        Assert.Equal("SIGA", data.Apellido);
        Assert.Equal("Administrador SIGA", data.NombreCompleto);
        Assert.Equal("admin@azafran.com", data.Email);
        Assert.Equal("12345678", data.Documento);
        Assert.Equal("3511112233", data.Telefono);
        Assert.Equal(1, data.RolId);
        Assert.Equal("ADMINISTRADOR", data.Rol);
        Assert.Equal(now, data.UltimoAcceso);
        Assert.Equal(EstadoUsuario.Activo, data.Estado);
        Assert.Contains("ROLES_VER", data.Permisos);
        Assert.Contains("ROLES_GESTIONAR", data.Permisos);
        Assert.Contains("ADMINISTRADOR", data.Roles);
    }

    [Fact]
    public async Task GetMeAsync_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetByIdWithRolesAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMeAsync(999));
    }
}
