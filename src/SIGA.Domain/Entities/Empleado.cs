namespace SIGA.Domain.Entities;

public class Empleado : Persona
{
    public string Legajo { get; set; } = string.Empty;
    public string Posicion { get; set; } = string.Empty;

    public Empleado()
    {
    }

    public Empleado(
        string nombre, 
        string apellido, 
        string documento, 
        string legajo, 
        string posicion, 
        string? telefono = null, 
        string? email = null)
    {
        Nombre = nombre;
        Apellido = apellido;
        Documento = documento;
        Legajo = legajo;
        Posicion = posicion;
        Telefono = telefono;
        Email = email;
        FechaCreacion = DateTime.UtcNow;
    }
}

