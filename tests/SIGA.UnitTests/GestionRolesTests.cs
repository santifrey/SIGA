using System.Linq.Expressions;
using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Roles;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionRolesTests
{
    private readonly Mock<IRolRepository> _rolRepoMock;
    private readonly Mock<IRepository<Permiso>> _permisoRepoMock;
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionRoles _service;

    public GestionRolesTests()
    {
        _rolRepoMock = new Mock<IRolRepository>();
        _permisoRepoMock = new Mock<IRepository<Permiso>>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionRoles(
            _rolRepoMock.Object,
            _permisoRepoMock.Object,
            _usuarioRepoMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnAllRolesWithPermissionsAndUserCount()
    {
        // Arrange
        var rol = new Rol
        {
            Id = 1,
            Nombre = "ADMINISTRADOR",
            Descripcion = "Admin del sistema",
            EsSistema = true
        };
        var perm = new Permiso { Id = 1, Codigo = "ROLES_VER" };
        rol.RolPermisos.Add(new RolPermiso { RolId = 1, Rol = rol, PermisoId = 1, Permiso = perm });
        rol.Usuarios.Add(new Usuario { Id = 1, IsDeleted = false });
        rol.Usuarios.Add(new Usuario { Id = 2, IsDeleted = true }); // Soft-deleted, shouldn't count

        _rolRepoMock.Setup(r => r.GetAllWithPermisosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rol> { rol });

        // Act
        var result = await _service.GetRolesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Single(result.Data);
        var dto = result.Data[0];
        Assert.Equal("ADMINISTRADOR", dto.Nombre);
        Assert.True(dto.EsSistema);
        Assert.Equal(1, dto.TotalUsuarios);
        Assert.Contains("ROLES_VER", dto.Permisos);
    }

    [Fact]
    public async Task GetRolByIdAsync_WhenRolExists_ShouldReturnRolWithPermissions()
    {
        // Arrange
        var rol = new Rol
        {
            Id = 2,
            Nombre = "VENDEDOR",
            Descripcion = "Ventas",
            EsSistema = true
        };
        rol.RolPermisos.Add(new RolPermiso { RolId = 2, Rol = rol, PermisoId = 5, Permiso = new Permiso { Id = 5, Codigo = "CLIENTES_VER" } });

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        // Act
        var result = await _service.GetRolByIdAsync(2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("VENDEDOR", result.Data.Nombre);
        Assert.Contains("CLIENTES_VER", result.Data.Permisos);
    }

    [Fact]
    public async Task GetRolByIdAsync_WhenRolNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rol?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetRolByIdAsync(99));
    }

    [Fact]
    public async Task CreateRolAsync_WithValidCustomRole_ShouldCreateAndAssignPermissions()
    {
        // Arrange
        var dto = new CrearRolDto
        {
            Nombre = "SUPERVISOR",
            Descripcion = "Supervisor de planta",
            Permisos = new List<string> { "STOCK_VER", "COSECHAS_VER" }
        };

        _rolRepoMock.Setup(r => r.GetByNombreAsync("SUPERVISOR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Rol?)null);

        var permisosList = new List<Permiso>
        {
            new Permiso { Id = 10, Codigo = "STOCK_VER" },
            new Permiso { Id = 11, Codigo = "COSECHAS_VER" }
        };
        _permisoRepoMock.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permisosList);

        Rol? capturedRol = null;
        _rolRepoMock.Setup(r => r.AddAsync(It.IsAny<Rol>(), It.IsAny<CancellationToken>()))
            .Callback<Rol, CancellationToken>((r, _) =>
            {
                r.Id = 100;
                capturedRol = r;
            })
            .ReturnsAsync((Rol r, CancellationToken _) => r);

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedRol);

        // Act
        var result = await _service.CreateRolAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(capturedRol);
        Assert.Equal("SUPERVISOR", capturedRol.Nombre);
        Assert.False(capturedRol.EsSistema);
        Assert.Equal(2, capturedRol.RolPermisos.Count);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRolAsync_WithDuplicateName_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CrearRolDto { Nombre = "SUPERVISOR" };
        _rolRepoMock.Setup(r => r.GetByNombreAsync("SUPERVISOR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Rol { Id = 10, Nombre = "SUPERVISOR" });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateRolAsync(dto));
    }

    [Fact]
    public async Task CreateRolAsync_WithReservedSystemRoleName_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CrearRolDto { Nombre = "ADMINISTRADOR" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateRolAsync(dto));
        Assert.Contains("reservado", ex.Message);
    }

    [Fact]
    public async Task UpdateRolAsync_WithValidData_ShouldUpdateRoleAndPermissions()
    {
        // Arrange
        var rol = new Rol
        {
            Id = 10,
            Nombre = "AUXILIAR",
            Descripcion = "Antigua descripción",
            EsSistema = false
        };

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        var dto = new ActualizarRolDto
        {
            Nombre = "AUXILIAR_GENERAL",
            Descripcion = "Nueva descripción",
            Permisos = new List<string> { "STOCK_VER" }
        };

        _permisoRepoMock.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permiso> { new Permiso { Id = 20, Codigo = "STOCK_VER" } });

        // Act
        var result = await _service.UpdateRolAsync(10, dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("AUXILIAR_GENERAL", rol.Nombre);
        Assert.Equal("Nueva descripción", rol.Descripcion);
        Assert.Single(rol.RolPermisos);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRolAsync_WhenAttemptingToRenameSystemRole_ShouldThrowValidationException()
    {
        // Arrange
        var rol = new Rol
        {
            Id = 1,
            Nombre = "ADMINISTRADOR",
            EsSistema = true
        };

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        var dto = new ActualizarRolDto
        {
            Nombre = "SUPER_ADMIN", // Intento de cambio de nombre
            Descripcion = "Nuevo admin"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateRolAsync(1, dto));
        Assert.Contains("No se permite modificar el nombre de un rol de sistema", ex.Message);
    }

    [Fact]
    public async Task DeleteRolAsync_WhenRolIsSystemRole_ShouldThrowValidationException()
    {
        // Arrange (Safeguard 1: EsSistema == true)
        var rol = new Rol
        {
            Id = 1,
            Nombre = "ADMINISTRADOR",
            EsSistema = true
        };

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteRolAsync(1));
        Assert.Contains("No se puede eliminar un rol protegido del sistema", ex.Message);
    }

    [Fact]
    public async Task DeleteRolAsync_WhenRolHasAssignedUsers_ShouldThrowValidationException()
    {
        // Arrange (Safeguard 2: Active users assigned)
        var rol = new Rol
        {
            Id = 10,
            Nombre = "CUSTOM_ROLE",
            EsSistema = false
        };

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        _usuarioRepoMock.Setup(u => u.ExistsAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteRolAsync(10));
        Assert.Contains("tiene usuarios asignados", ex.Message);
    }

    [Fact]
    public async Task DeleteRolAsync_WhenCustomRoleWithNoUsers_ShouldDeleteSuccessfully()
    {
        // Arrange
        var rol = new Rol
        {
            Id = 10,
            Nombre = "CUSTOM_ROLE",
            EsSistema = false
        };

        _rolRepoMock.Setup(r => r.GetByIdWithPermisosAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rol);

        _usuarioRepoMock.Setup(u => u.ExistsAsync(It.IsAny<Expression<Func<Usuario, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteRolAsync(10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        _rolRepoMock.Verify(r => r.DeleteAsync(rol, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
