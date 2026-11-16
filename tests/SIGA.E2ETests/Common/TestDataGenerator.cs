namespace SIGA.E2ETests.Common;

public static class TestDataGenerator
{
    public static string GenerateUsername() => $"user_{Guid.NewGuid().ToString("N")[..8]}";

    public static string GenerateEmail() => $"test_{Guid.NewGuid().ToString("N")[..8]}@siga-test.com";

    public static string GenerateDocumento() => $"{Random.Shared.Next(10000000, 99999999)}";

    public static string GenerateCuit() => $"20{Random.Shared.Next(10000000, 99999999)}9";

    public static string GenerateRoleName() => $"ROL_{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public static CreateEmpleadoDto CreateSampleEmpleadoDto(int rolId = 2)
    {
        var uid = Guid.NewGuid().ToString("N")[..6];
        return new CreateEmpleadoDto
        {
            Nombre = $"Empleado_{uid}",
            Apellido = "Test",
            Email = $"emp_{uid}@siga-test.com",
            Documento = GenerateDocumento(),
            Telefono = "123456789",
            Username = $"emp_{uid}",
            Password = "Password123!",
            RolId = rolId
        };
    }

    public static CreateClienteDto CreateSampleClienteDto()
    {
        var uid = Guid.NewGuid().ToString("N")[..6];
        return new CreateClienteDto
        {
            Nombre = $"Cliente {uid}",
            Cuit = GenerateCuit(),
            Email = $"cli_{uid}@cliente-test.com",
            Telefono = "987654321",
            Direccion = "Calle Falsa 123"
        };
    }
}
