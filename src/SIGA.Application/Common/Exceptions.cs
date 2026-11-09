namespace SIGA.Application.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public List<string>? Errors { get; }

    public AppException(string message, int statusCode = 400, List<string>? errors = null) 
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404)
    {
    }

    public NotFoundException(string entityName, object key) 
        : base($"No se encontró la entidad '{entityName}' con identificador ({key}).", 404)
    {
    }
}

public class ValidationException : AppException
{
    public ValidationException(string message, List<string>? errors = null) 
        : base(message, 400, errors)
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "No autorizado.") 
        : base(message, 401)
    {
    }
}
