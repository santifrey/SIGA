using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Transferencias;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

public class GestionTransferenciasTests
{
    private readonly Mock<ITransferenciaLoteRepository> _transferenciaRepoMock;
    private readonly Mock<ILoteRepository> _loteRepoMock;
    private readonly Mock<IRepository<ParticipanteComercial>> _participanteRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GestionTransferencias _service;

    public GestionTransferenciasTests()
    {
        _transferenciaRepoMock = new Mock<ITransferenciaLoteRepository>();
        _loteRepoMock = new Mock<ILoteRepository>();
        _participanteRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _service = new GestionTransferencias(
            _transferenciaRepoMock.Object,
            _loteRepoMock.Object,
            _participanteRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task TransferirLoteAsync_WhenValid_ShouldCreateConfirmedTransfer()
    {
        // Arrange: Lote 1 con titular original Cliente 2
        var lote = new Lote(1, 2, 1, 1)
        {
            Id = 1,
            Estado = EstadoLote.Plantado,
            CantidadPlantadaTotal = 1000,
            Temporada = new Temporada("2026", 1) { Estado = EstadoTemporada.EnCrecimiento }
        };

        _loteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lote);

        _transferenciaRepoMock.Setup(r => r.HasPendingTransferAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Nuevo titular es Cliente 3
        _participanteRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ParticipanteComercial, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new TransferirLoteDto
        {
            LoteId = 1,
            NuevoTitularId = 3,
            Observaciones = "Venta completa de lote plantado"
        };

        // Act
        var result = await _service.TransferirLoteAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(EstadoTransferencia.Confirmada, result.Data.Estado);
        Assert.Equal(2, result.Data.TitularAnteriorId);
        Assert.Equal(3, result.Data.NuevoTitularId);
        _transferenciaRepoMock.Verify(r => r.AddAsync(It.IsAny<TransferenciaLote>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferirLoteAsync_WhenNuevoTitularIsSameAsCurrent_ShouldThrowOperacionInvalidaException()
    {
        // Arrange
        var lote = new Lote(1, 2, 1, 1)
        {
            Id = 1,
            Estado = EstadoLote.Plantado,
            Temporada = new Temporada("2026", 1) { Estado = EstadoTemporada.EnCrecimiento }
        };

        _loteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lote);

        var dto = new TransferirLoteDto
        {
            LoteId = 1,
            NuevoTitularId = 2 // Mismo titular
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperacionInvalidaException>(() => _service.TransferirLoteAsync(dto, 1));
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferirLoteAsync_WhenSeasonIsClosed_ShouldThrowOperacionInvalidaException()
    {
        // Arrange
        var lote = new Lote(1, 2, 1, 1)
        {
            Id = 1,
            Estado = EstadoLote.Plantado,
            Temporada = new Temporada("2025", 1) { Estado = EstadoTemporada.Cerrada }
        };

        _loteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lote);

        var dto = new TransferirLoteDto
        {
            LoteId = 1,
            NuevoTitularId = 3
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperacionInvalidaException>(() => _service.TransferirLoteAsync(dto, 1));
    }

    [Fact]
    public async Task TransferirLoteAsync_WhenLoteIsAlreadyCosechado_ShouldThrowOperacionInvalidaException()
    {
        // Arrange
        var lote = new Lote(1, 2, 1, 1)
        {
            Id = 1,
            Estado = EstadoLote.Cosechado,
            Temporada = new Temporada("2026", 1) { Estado = EstadoTemporada.Cosecha }
        };

        _loteRepoMock.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lote);

        var dto = new TransferirLoteDto
        {
            LoteId = 1,
            NuevoTitularId = 3
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperacionInvalidaException>(() => _service.TransferirLoteAsync(dto, 1));
    }

    [Fact]
    public async Task ResolverTitularVigenteId_WithMultipleSequentialTransfers_ShouldReturnLatestConfirmed()
    {
        // Arrange: Titular original Cliente 2
        var lote = new Lote(1, 2, 1, 1) { Id = 10 };

        // Transferencia 1 (Confirmada): 2 -> 3 en t1
        var t1 = new TransferenciaLote(10, 2, 3, 1)
        {
            Id = 1,
            Fecha = DateTime.UtcNow.AddDays(-5),
            Estado = EstadoTransferencia.Confirmada
        };

        // Transferencia 2 (Confirmada): 3 -> 4 (o Empresa 1) en t2
        var t2 = new TransferenciaLote(10, 3, 1, 1)
        {
            Id = 2,
            Fecha = DateTime.UtcNow.AddDays(-2),
            Estado = EstadoTransferencia.Confirmada
        };

        // Transferencia 3 (Anulada): 1 -> 5
        var t3 = new TransferenciaLote(10, 1, 5, 1)
        {
            Id = 3,
            Fecha = DateTime.UtcNow.AddDays(-1),
            Estado = EstadoTransferencia.Anulada
        };

        lote.Transferencias.Add(t1);
        lote.Transferencias.Add(t2);
        lote.Transferencias.Add(t3);

        // Act
        var titularVigenteId = lote.ResolverTitularVigenteId();

        // Assert: El titular vigente debe ser el nuevo titular de la última transferencia CONFIRMADA (1, Empresa)
        Assert.Equal(1, titularVigenteId);
    }
}
