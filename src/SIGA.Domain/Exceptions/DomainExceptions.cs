namespace SIGA.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class StockInsuficienteException : DomainException
{
    public int ParticipanteId { get; }
    public int ClaseBulbo { get; }
    public int CantidadSolicitada { get; }
    public int StockDisponible { get; }

    public StockInsuficienteException(string message) : base(message)
    {
    }

    public StockInsuficienteException(int participanteId, int claseBulbo, int cantidadSolicitada, int stockDisponible)
        : base($"Stock insuficiente para el participante {participanteId} en la Clase {claseBulbo}. Solicitado: {cantidadSolicitada}, Disponible: {stockDisponible}.")
    {
        ParticipanteId = participanteId;
        ClaseBulbo = claseBulbo;
        CantidadSolicitada = cantidadSolicitada;
        StockDisponible = stockDisponible;
    }
}

public class OperacionInvalidaException : DomainException
{
    public OperacionInvalidaException(string message) : base(message)
    {
    }
}
