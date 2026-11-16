using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using SIGA.API.Authorization;
using Xunit;

namespace SIGA.UnitTests;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler;

    public PermissionAuthorizationHandlerTests()
    {
        _handler = new PermissionAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserHasRequiredPermissionClaim_ShouldSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "vendedor"),
            new Claim("permiso", "EMPLEADOS_VER")
        }, "TestAuthType"));

        var requirement = new PermissionRequirement("EMPLEADOS_VER");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserLacksRequiredPermissionClaim_ShouldNotSucceed()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "vendedor"),
            new Claim("permiso", "STOCK_VER")
        }, "TestAuthType"));

        var requirement = new PermissionRequirement("EMPLEADOS_VER");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserHasAdministratorRole_ShouldBypassAndSucceed()
    {
        // Arrange: Usuario con rol ADMINISTRADOR pero SIN ninguna claim de permiso
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "admin"),
            new Claim(ClaimTypes.Role, "ADMINISTRADOR")
        }, "TestAuthType"));

        var requirement = new PermissionRequirement("CUALQUIER_PERMISO_DEL_SISTEMA");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserIsNotAuthenticated_ShouldNotSucceed()
    {
        // Arrange: ClaimsPrincipal sin autenticar
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var requirement = new PermissionRequirement("ROLES_VER");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMultiplePermissionsInRequirement_ShouldSucceedIfUserHasAny()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "secretaria"),
            new Claim("permiso", "MOVCOM_APROBAR")
        }, "TestAuthType"));

        var requirement = new PermissionRequirement(new[] { "MOVCOM_FINALIZAR", "MOVCOM_APROBAR" });
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionPolicyProvider_GetPolicyAsync_WithPermissionPrefix_ShouldBuildPolicyWithRequirement()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetPolicyAsync("PERMISO_ROLES_VER");

        // Assert
        Assert.NotNull(policy);
        Assert.Single(policy.Requirements);
        var req = Assert.IsType<PermissionRequirement>(policy.Requirements.First());
        Assert.Equal("ROLES_VER", req.Permiso);
    }
}
