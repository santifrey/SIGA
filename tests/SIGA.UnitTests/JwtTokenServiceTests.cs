using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using SIGA.Domain.Entities;
using SIGA.Infrastructure.Security;
using Xunit;

namespace SIGA.UnitTests;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtTokenService _jwtService;

    public JwtTokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["JwtSettings:SecretKey"])
            .Returns("SIGA_Super_Secret_Key_For_Unit_Testing_1234567890!");
        _configurationMock.Setup(c => c["JwtSettings:Issuer"])
            .Returns("SIGA.API.Test");
        _configurationMock.Setup(c => c["JwtSettings:Audience"])
            .Returns("SIGA.Frontend.Test");
        _configurationMock.Setup(c => c["JwtSettings:ExpirationHours"])
            .Returns("2");

        _jwtService = new JwtTokenService(_configurationMock.Object);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtWithExpectedClaims()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 10,
            PersonaId = 5,
            Username = "juanperez",
            Persona = new Persona
            {
                Id = 5,
                Nombre = "Juan",
                Apellido = "Pérez",
                Documento = "30123456",
                Email = "juan.perez@azafranmediterraneo.com"
            }
        };
        var roles = new List<string> { "ADMINISTRADOR", "VENDEDOR" };

        // Act
        var tokenString = _jwtService.GenerateToken(usuario, roles);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal("SIGA.API.Test", jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == "SIGA.Frontend.Test");

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
        Assert.NotNull(subClaim);
        Assert.Equal("10", subClaim.Value);

        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "unique_name");
        Assert.NotNull(nameClaim);
        Assert.Equal("juanperez", nameClaim.Value);

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email");
        Assert.NotNull(emailClaim);
        Assert.Equal("juan.perez@azafranmediterraneo.com", emailClaim.Value);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).ToList();
        Assert.Contains("ADMINISTRADOR", roleClaims);
        Assert.Contains("VENDEDOR", roleClaims);
    }

    [Fact]
    public void GenerateToken_WhenSecretKeyIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyConfigMock = new Mock<IConfiguration>();
        emptyConfigMock.Setup(c => c["JwtSettings:SecretKey"]).Returns(string.Empty);
        var service = new JwtTokenService(emptyConfigMock.Object);

        var usuario = new Usuario { Id = 1, PersonaId = 1, Username = "admin" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(usuario, new[] { "ADMINISTRADOR" }));
    }
}
