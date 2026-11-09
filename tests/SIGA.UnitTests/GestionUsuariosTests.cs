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
            Persona = new Persona { Id = 1, Nombre = "Admin", Apellido = "SIGA", Email = "admin@siga.com" }
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameWithRolesAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock.Setup(h => h.VerifyPassword("Admin123!", "hashedPassword"))
            .Returns(true);

        _tokenServiceMock.Setup(t => t.GenerateToken(usuario, It.IsAny<IEnumerable<string>>()))
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
}
