using System.Linq.Expressions;
using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Empleados;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionEmpleadosTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IPersonaRepository> _personaRepoMock;
    private readonly Mock<IRolRepository> _rolRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionEmpleados _service;

    public GestionEmpleadosTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _personaRepoMock = new Mock<IPersonaRepository>();
        _rolRepoMock = new Mock<IRolRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionEmpleados(
            _usuarioRepoMock.Object,
            _personaRepoMock.Object,
            _rolRepoMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task GetEmpleadosAsync_ShouldReturnStaffAndExcludeCliente()
    {
        // Arrange
        var staffUser = new Usuario
        {
            Id = 1,
            Username = "vendedor1",
            RolId = 2,
            Rol = new Rol { Id = 2, Nombre = "VENDEDOR" },
            Persona = new Persona { Id = 1, Nombre = "Carlos", Apellido = "Ventas", Documento = "111" },
            IsDeleted = false
        };

        var clienteUser = new Usuario
        {
            Id = 2,
            Username = "cliente1",
            RolId = 4,
            Rol = new Rol { Id = 4, Nombre = "CLIENTE" },
            Persona = new Persona { Id = 2, Nombre = "Ana", Apellido = "Cliente", Documento = "222" },
            IsDeleted = false
        };

        _usuarioRepoMock.Setup(u => u.GetAllWithRolesAndPersonaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Usuario> { staffUser, clienteUser });

        // Act
        var result = await _service.GetEmpleadosAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal("vendedor1", result.Data[0].Username);
    }

    [Fact]
    public async Task GetEmpleadoByIdAsync_WhenEmployeeExists_ShouldReturnDetails()
    {
        // Arrange
        var user = new Usuario
        {
            Id = 1,
            Username = "secretaria1",
            RolId = 3,
            Rol = new Rol { Id = 3, Nombre = "SECRETARIA" },
            Persona = new Persona { Id = 5, Nombre = "Maria", Apellido = "Gomez", Documento = "333" },
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetEmpleadoByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("secretaria1", result.Data.Username);
        Assert.Equal("SECRETARIA", result.Data.Rol);
    }

    [Fact]
    public async Task GetEmpleadoByIdAsync_WhenUserHasClienteRole_ShouldThrowNotFoundException()
    {
        // Arrange
        var user = new Usuario
        {
            Id = 2,
            Username = "cliente1",
            RolId = 4,
            Rol = new Rol { Id = 4, Nombre = "CLIENTE" }
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetEmpleadoByIdAsync(2));
    }

    [Fact]
    public async Task CreateEmpleadoAsync_WithValidData_ShouldCreatePersonaAndUsuarioInTransaction()
    {
        // Arrange
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Lucas",
            Apellido = "Silva",
            Documento = "44445555",
            Telefono = "123456",
            Email = "lucas@siga.com",
            Username = "lsilva",
            Password = "Password123!",
            RolId = 2 // VENDEDOR
        };

        _rolRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 2, Nombre = "VENDEDOR" });

        _usuarioRepoMock.Setup(u => u.GetByUsernameWithRolesAsync("lsilva", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _personaRepoMock.Setup(p => p.GetByDocumentoAsync("44445555", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Persona?)null);

        _passwordHasherMock.Setup(h => h.HashPassword("Password123!"))
            .Returns("hashedPassword");

        Persona? createdPersona = null;
        _personaRepoMock.Setup(p => p.AddAsync(It.IsAny<Persona>(), It.IsAny<CancellationToken>()))
            .Callback<Persona, CancellationToken>((p, _) =>
            {
                p.Id = 15;
                createdPersona = p;
            })
            .ReturnsAsync((Persona p, CancellationToken _) => p);

        Usuario? createdUsuario = null;
        _usuarioRepoMock.Setup(u => u.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) =>
            {
                u.Id = 20;
                createdUsuario = u;
            })
            .ReturnsAsync((Usuario u, CancellationToken _) => u);

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => createdUsuario);

        // Act
        var result = await _service.CreateEmpleadoAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(createdPersona);
        Assert.NotNull(createdUsuario);
        Assert.Equal("lsilva", createdUsuario.Username);
        Assert.Equal(2, createdUsuario.RolId);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEmpleadoAsync_WhenRoleIsCliente_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Lucas",
            Apellido = "Silva",
            Documento = "44445555",
            Username = "lsilva",
            Password = "Password123!",
            RolId = 4
        };

        _rolRepoMock.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 4, Nombre = "CLIENTE" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateEmpleadoAsync(dto));
        Assert.Contains("No se puede asignar el rol de CLIENTE a un empleado", ex.Message);
    }

    [Fact]
    public async Task CreateEmpleadoAsync_WhenUsernameAlreadyExists_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Lucas",
            Apellido = "Silva",
            Documento = "44445555",
            Username = "lsilva",
            Password = "Password123!",
            RolId = 2
        };

        _rolRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 2, Nombre = "VENDEDOR" });

        _usuarioRepoMock.Setup(u => u.GetByUsernameWithRolesAsync("lsilva", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = 99, Username = "lsilva" });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateEmpleadoAsync(dto));
    }

    [Fact]
    public async Task CreateEmpleadoAsync_WhenDocumentoAlreadyExists_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Lucas",
            Apellido = "Silva",
            Documento = "44445555",
            Username = "lsilva",
            Password = "Password123!",
            RolId = 2
        };

        _rolRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 2, Nombre = "VENDEDOR" });

        _usuarioRepoMock.Setup(u => u.GetByUsernameWithRolesAsync("lsilva", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _personaRepoMock.Setup(p => p.GetByDocumentoAsync("44445555", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Persona { Id = 50, Documento = "44445555" });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateEmpleadoAsync(dto));
    }

    [Fact]
    public async Task UpdateEmpleadoAsync_WithValidData_ShouldUpdatePersonaAndUsuario()
    {
        // Arrange
        var persona = new Persona { Id = 1, Nombre = "Juan", Apellido = "Perez", Documento = "111" };
        var usuario = new Usuario
        {
            Id = 5,
            Username = "jperez",
            PersonaId = 1,
            Persona = persona,
            RolId = 2,
            Rol = new Rol { Id = 2, Nombre = "VENDEDOR" },
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        var newRol = new Rol { Id = 3, Nombre = "SECRETARIA" };
        _rolRepoMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRol);

        var dto = new ActualizarEmpleadoDto
        {
            Nombre = "Juan Carlos",
            Apellido = "Perez",
            Documento = "111",
            RolId = 3,
            Estado = EstadoUsuario.Activo
        };

        // Act
        var result = await _service.UpdateEmpleadoAsync(5, dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Juan Carlos", persona.Nombre);
        Assert.Equal(3, usuario.RolId);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateEmpleadoAsync_WhenAssigningClienteRole_ShouldThrowValidationException()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 5,
            RolId = 2,
            Rol = new Rol { Id = 2, Nombre = "VENDEDOR" },
            Persona = new Persona { Id = 1 }
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _rolRepoMock.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 4, Nombre = "CLIENTE" });

        var dto = new ActualizarEmpleadoDto { RolId = 4, Nombre = "J", Apellido = "P", Documento = "111" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateEmpleadoAsync(5, dto));
    }

    [Fact]
    public async Task UpdateEmpleadoAsync_WhenDemotingOrDeactivatingLastActiveAdmin_ShouldThrowValidationException()
    {
        // Arrange: Solo hay 1 administrador activo
        var adminUser = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo,
            Persona = new Persona { Id = 1, Documento = "000" }
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        _rolRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 2, Nombre = "VENDEDOR" });

        _usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 active admin

        var dto = new ActualizarEmpleadoDto
        {
            Nombre = "Admin",
            Apellido = "SIGA",
            Documento = "000",
            RolId = 2, // Intentando cambiar rol de ADMIN a VENDEDOR
            Estado = EstadoUsuario.Activo
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateEmpleadoAsync(1, dto));
        Assert.Contains("No se puede desactivar el único administrador activo", ex.Message);
    }

    [Fact]
    public async Task DeleteEmpleadoAsync_WhenDeletingLastActiveAdmin_ShouldThrowValidationException()
    {
        // Arrange: Solo hay 1 administrador activo
        var adminUser = new Usuario
        {
            Id = 1,
            Username = "admin",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        _usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // 1 active admin

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteEmpleadoAsync(1));
        Assert.Equal("No se puede desactivar el único administrador activo.", ex.Message);
    }

    [Fact]
    public async Task DeleteEmpleadoAsync_WhenMultipleActiveAdminsExist_ShouldSoftDeleteSettingEstadoInactivo()
    {
        // Arrange: Hay 2 administradores activos
        var adminUser = new Usuario
        {
            Id = 1,
            Username = "admin1",
            RolId = 1,
            Rol = new Rol { Id = 1, Nombre = "ADMINISTRADOR" },
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        _usuarioRepoMock.Setup(u => u.CountAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // 2 active admins

        // Act
        var result = await _service.DeleteEmpleadoAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoUsuario.Inactivo, adminUser.Estado);
        _usuarioRepoMock.Verify(u => u.UpdateAsync(adminUser, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEmpleadoAsync_WhenEmployeeIsNonAdmin_ShouldSoftDeleteSettingEstadoInactivo()
    {
        // Arrange
        var employee = new Usuario
        {
            Id = 10,
            Username = "vendedor",
            RolId = 2,
            Rol = new Rol { Id = 2, Nombre = "VENDEDOR" },
            Estado = EstadoUsuario.Activo
        };

        _usuarioRepoMock.Setup(u => u.GetByIdWithRolesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _service.DeleteEmpleadoAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoUsuario.Inactivo, employee.Estado);
        _usuarioRepoMock.Verify(u => u.UpdateAsync(employee, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
