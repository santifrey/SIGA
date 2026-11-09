using SIGA.Application.Common;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Interfaces;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Interfaces;

namespace SIGA.Application.Services;

public class GestionStock : IGestionStock
{
    private readonly IMovimientoBulbosRepository _movimientoBulbosRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IRepository<ParticipanteComercial> _participanteRepository;

    private static readonly ClaseBulbo[] TodasLasClases =
    {
        ClaseBulbo.Clase0,
        ClaseBulbo.Clase1,
        ClaseBulbo.Clase2,
        ClaseBulbo.Clase3,
        ClaseBulbo.Clase4
    };

    public GestionStock(
        IMovimientoBulbosRepository movimientoBulbosRepository,
        IClienteRepository clienteRepository,
        IRepository<ParticipanteComercial> participanteRepository)
    {
        _movimientoBulbosRepository = movimientoBulbosRepository;
        _clienteRepository = clienteRepository;
        _participanteRepository = participanteRepository;
    }

    public async Task<ApiResponse<StockClienteDto>> ConsultarStockPorClienteAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(clienteId, cancellationToken);
        string clienteNombre;
        string codigoCliente;

        if (cliente != null)
        {
            clienteNombre = cliente.Persona != null ? cliente.Persona.NombreCompleto : $"Cliente #{cliente.Id}";
            codigoCliente = cliente.CodigoCliente;
        }
        else
        {
            var participante = await _participanteRepository.GetByIdAsync(clienteId, cancellationToken);
            if (participante is Empresa empresa)
            {
                clienteNombre = empresa.RazonSocial;
                codigoCliente = "EMPRESA";
            }
            else
            {
                throw new NotFoundException("Participante", clienteId);
            }
        }

        var saldoPorClase = await ObtenerSaldoPorClaseAsync(clienteId, cancellationToken);

        var listadoClases = TodasLasClases.Select(clase => new StockPorClaseDto
        {
            Clase = clase,
            NombreClase = FormatearNombreClase(clase),
            Cantidad = saldoPorClase.TryGetValue(clase, out var cant) ? cant : 0
        }).ToList();

        var dto = new StockClienteDto
        {
            ClienteId = clienteId,
            ClienteNombre = clienteNombre,
            CodigoCliente = codigoCliente,
            StockPorClase = listadoClases,
            TotalBulbos = listadoClases.Sum(s => s.Cantidad)
        };

        return ApiResponse<StockClienteDto>.Ok(dto, "Stock consultado exitosamente.");
    }

    public async Task<ApiResponse<DisponibilidadStockResponseDto>> VerificarDisponibilidadAsync(
        int clienteId, 
        ClaseBulbo clase, 
        int cantidad, 
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.GetByIdWithPersonaAsync(clienteId, cancellationToken);
        if (cliente == null)
        {
            var participante = await _participanteRepository.GetByIdAsync(clienteId, cancellationToken);
            if (participante == null)
                throw new NotFoundException("Participante", clienteId);
        }

        var saldoPorClase = await ObtenerSaldoPorClaseAsync(clienteId, cancellationToken);
        var stockDisponible = saldoPorClase.TryGetValue(clase, out var stock) ? stock : 0;

        var response = new DisponibilidadStockResponseDto
        {
            ClienteId = clienteId,
            Clase = clase,
            CantidadSolicitada = cantidad,
            StockDisponible = stockDisponible,
            Disponible = stockDisponible >= cantidad
        };

        return ApiResponse<DisponibilidadStockResponseDto>.Ok(response);
    }

    public async Task<Dictionary<ClaseBulbo, int>> ObtenerSaldoPorClaseAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var movimientosConfirmados = await _movimientoBulbosRepository.GetConfirmadosByParticipanteAsync(clienteId, cancellationToken);

        var saldo = TodasLasClases.ToDictionary(c => c, _ => 0);

        foreach (var mov in movimientosConfirmados)
        {
            if (mov.Estado != EstadoMovimiento.Confirmado)
                continue;

            // Entrada: Destino es el cliente
            if (mov.DestinoId == clienteId)
            {
                foreach (var cant in mov.Cantidades)
                {
                    if (saldo.ContainsKey(cant.ClaseBulbo))
                    {
                        saldo[cant.ClaseBulbo] += cant.Cantidad;
                    }
                    else
                    {
                        saldo[cant.ClaseBulbo] = cant.Cantidad;
                    }
                }
            }

            // Salida: Origen es el cliente
            if (mov.OrigenId == clienteId)
            {
                foreach (var cant in mov.Cantidades)
                {
                    if (saldo.ContainsKey(cant.ClaseBulbo))
                    {
                        saldo[cant.ClaseBulbo] -= cant.Cantidad;
                    }
                    else
                    {
                        saldo[cant.ClaseBulbo] = -cant.Cantidad;
                    }
                }
            }
        }

        return saldo;
    }

    private static string FormatearNombreClase(ClaseBulbo clase)
    {
        return clase switch
        {
            ClaseBulbo.Clase0 => "Clase 0 (Sin calificar)",
            ClaseBulbo.Clase1 => "Clase 1 (Calibre menor)",
            ClaseBulbo.Clase2 => "Clase 2 (Calibre estándar)",
            ClaseBulbo.Clase3 => "Clase 3 (Calibre superior)",
            ClaseBulbo.Clase4 => "Clase 4 (Calibre premium)",
            _ => clase.ToString()
        };
    }
}
