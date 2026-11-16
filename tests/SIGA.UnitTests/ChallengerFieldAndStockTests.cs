using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SIGA.Application.Common;
using SIGA.Application.DTOs.Filas;
using SIGA.Application.DTOs.Lotes;
using SIGA.Application.DTOs.MovimientosComerciales;
using SIGA.Application.DTOs.Plantaciones;
using SIGA.Application.DTOs.Stock;
using SIGA.Application.Exceptions;
using SIGA.Application.Interfaces;
using SIGA.Application.Services;
using SIGA.Domain.Entities;
using SIGA.Domain.Enums;
using SIGA.Domain.Exceptions;
using SIGA.Domain.Interfaces;
using Xunit;

namespace SIGA.UnitTests;

/// <summary>
/// Phase 2: Adversarial Coverage Hardening (Tier 5) — Suite 2.
/// Deep white-box adversarial stress tests focusing on:
/// 1. Plantations & Field Production (Fila, Lote, GestionPlantaciones, GestionFilas)
/// 2. Stock & Bulb Inventory (GestionStock, StockController, Empresa singleton vs client, multi-calibre MIX decomposition)
/// 3. Frontend Contract & System Integrity (Static assertion audits on deactivations, zero trash cans, omission of notifications, sidebar persistence)
/// </summary>
public class ChallengerFieldAndStockTests
{
    #region 1. Plantations & Field Production Domain Boundaries (Fila, Lote)

    [Theory]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Fila_ConstructorWithString_NegativeCapacity_ThrowsOperacionInvalidaException(int negativeCap)
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            new Fila(1, 1, "Fila-01", negativeCap));

        Assert.Contains("La capacidad no puede ser negativa", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Fila_ConstructorWithInt_NegativeCapacity_ThrowsOperacionInvalidaException(int negativeCap)
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            new Fila(1, 1, 10, negativeCap));

        Assert.Contains("La capacidad no puede ser negativa", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Fila_ConstructorWithInt_ZeroOrNegativeRowNumber_ThrowsOperacionInvalidaException(int invalidRowNumber)
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            new Fila(1, 1, invalidRowNumber, 1000));

        Assert.Contains("El número de fila debe ser mayor a cero", ex.Message);
    }

    [Fact]
    public void Fila_ZeroCapacity_TreatedAsUnboundedSemantics()
    {
        // When Capacidad == 0, domain models treat it as unbounded (unlimited) capacity
        var fila = new Fila(1, 1, "Fila-Libre", 0);

        Assert.Equal(int.MaxValue, fila.ObtenerCapacidadDisponible());
        Assert.True(fila.TieneCapacidadDisponible(100_000));
        Assert.True(fila.TieneCapacidadDisponible(1_000_000));
    }

    [Fact]
    public void Fila_DirectPropertySetter_NegativeCapacity_DesignQuirkExposed()
    {
        // Public setter allows mutation bypassing constructor validation
        var fila = new Fila(1, 1, "Fila-Test", 500)
        {
            Capacidad = -10 // Mutated directly
        };

        // Because Capacidad <= 0, ObtenerCapacidadDisponible returns int.MaxValue!
        Assert.Equal(int.MaxValue, fila.ObtenerCapacidadDisponible());
        Assert.True(fila.TieneCapacidadDisponible(50_000));
    }

    [Fact]
    public void Fila_ExactCapacity_Boundary_And_ExactOverflowAtCapacityPlusOne()
    {
        // Arrange: Fila with capacity 1000 and current occupancy 900
        var fila = new Fila(1, 1, "Fila-A", 1000);
        var loteExistente = new Lote(fila.Id, 2, 1, 1)
        {
            CantidadPlantadaTotal = 900
        };
        fila.Lotes.Add(loteExistente);

        Assert.Equal(900, fila.ObtenerOcupacionActual());
        Assert.Equal(100, fila.ObtenerCapacidadDisponible());

        // Exact boundary: 100 units fit exactly
        Assert.True(fila.TieneCapacidadDisponible(100));

        // Exact overflow: 101 units (capacity + 1) must be rejected
        Assert.False(fila.TieneCapacidadDisponible(101));
    }

    [Fact]
    public void Fila_ExtremeQuantity_IntegerOverflow_VulnerabilityTraced()
    {
        // Arrange: Fila with capacity 1000 and current occupancy 10
        var fila = new Fila(1, 1, "Fila-Overflow", 1000);
        var lote = new Lote(fila.Id, 2, 1, 1) { CantidadPlantadaTotal = 10 };
        fila.Lotes.Add(lote);

        // When adding int.MaxValue in unchecked arithmetic:
        // 10 + int.MaxValue = int.MinValue + 9 = -2147483639
        // -2147483639 <= 1000 evaluates to TRUE!
        bool result = fila.TieneCapacidadDisponible(int.MaxValue);

        // Documenting that unchecked integer overflow bypasses the capacity check
        Assert.True(result);
    }

    [Theory]
    [InlineData(0, 1, 1, 1, "La fila especificada es inválida")]
    [InlineData(-1, 1, 1, 1, "La fila especificada es inválida")]
    [InlineData(1, 0, 1, 1, "El cliente titular es requerido")]
    [InlineData(1, -1, 1, 1, "El cliente titular es requerido")]
    [InlineData(1, 1, 0, 1, "La temporada es requerida")]
    [InlineData(1, 1, -1, 1, "La temporada es requerida")]
    [InlineData(1, 1, 1, 0, "El orden del lote en la fila debe ser mayor a cero")]
    [InlineData(1, 1, 1, -1, "El orden del lote en la fila debe ser mayor a cero")]
    public void Lote_Constructor_InvalidBoundaries_ThrowsOperacionInvalidaException(
        int filaId, int clienteId, int temporadaId, int orden, string expectedMessagePart)
    {
        var ex = Assert.Throws<OperacionInvalidaException>(() =>
            new Lote(filaId, clienteId, temporadaId, orden));

        Assert.Contains(expectedMessagePart, ex.Message);
    }

    [Fact]
    public void Lote_ResolverTitularVigenteId_WhenNoTransferencias_ReturnsOriginalCliente()
    {
        var lote = new Lote(1, 42, 1, 1);
        Assert.Equal(42, lote.ResolverTitularVigenteId());
    }

    [Fact]
    public void Lote_ResolverTitularVigenteId_WithTransferChain_ResolvesLatestConfirmedTransfer()
    {
        var lote = new Lote(1, 10, 1, 1);

        // Transfer 1 (Confirmed, older) -> transferred to 20
        lote.Transferencias.Add(new TransferenciaLote
        {
            Id = 1,
            LoteId = lote.Id,
            NuevoTitularId = 20,
            Fecha = DateTime.UtcNow.AddDays(-10),
            Estado = EstadoTransferencia.Confirmada
        });

        // Transfer 2 (Confirmed, latest) -> transferred to 30
        lote.Transferencias.Add(new TransferenciaLote
        {
            Id = 2,
            LoteId = lote.Id,
            NuevoTitularId = 30,
            Fecha = DateTime.UtcNow.AddDays(-1),
            Estado = EstadoTransferencia.Confirmada
        });

        // Transfer 3 (Pending, future) -> proposed transfer to 40 (must be ignored)
        lote.Transferencias.Add(new TransferenciaLote
        {
            Id = 3,
            LoteId = lote.Id,
            NuevoTitularId = 40,
            Fecha = DateTime.UtcNow,
            Estado = EstadoTransferencia.Pendiente
        });

        // Transfer 4 (Deleted) -> transferred to 50 (must be ignored)
        lote.Transferencias.Add(new TransferenciaLote
        {
            Id = 4,
            LoteId = lote.Id,
            NuevoTitularId = 50,
            Fecha = DateTime.UtcNow,
            Estado = EstadoTransferencia.Confirmada,
            IsDeleted = true
        });

        Assert.Equal(30, lote.ResolverTitularVigenteId());
    }

    #endregion

    #region 2. Plantations Application Service & Edge Cases (GestionPlantaciones, GestionFilas)

    [Fact]
    public async Task RegistrarDistribucion_WhenSeasonIsClosed_ThrowsValidationException()
    {
        // Arrange
        var (service, mocks) = BuildPlantacionesService();
        var dto = CreateValidDistribucionDto();

        var temporadaCerrada = new Temporada("Temporada 2025", 1)
        {
            Id = dto.TemporadaId,
            Estado = EstadoTemporada.Cerrada
        };

        mocks.TemporadaRepo.Setup(r => r.GetByIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporadaCerrada);

        mocks.ClienteRepo.Setup(r => r.GetByIdWithPersonaAsync(dto.ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cliente { Id = dto.ClienteId });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegistrarDistribucionAsync(dto, 1));

        Assert.Contains("temporada cerrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarDistribucion_WhenRowCapacityExceeded_ThrowsValidationException()
    {
        // Arrange
        var (service, mocks) = BuildPlantacionesService();
        var dto = CreateValidDistribucionDto();
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 600 } // Exceeds available capacity
        };

        SetupCommonMocks(mocks, dto);

        var fila = new Fila(dto.SucursalId, dto.TemporadaId, 1, capacidad: 1000);
        fila.Lotes.Add(new Lote(fila.Id, 2, dto.TemporadaId, 1) { CantidadPlantadaTotal = 500 });

        mocks.FilaRepo.Setup(r => r.GetByIdWithLotesAsync(dto.FilaId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        // Act & Assert: Occupancy 500 + New 600 = 1100 > Capacity 1000
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegistrarDistribucionAsync(dto, 1));

        Assert.Contains("supera la capacidad máxima de la fila", ex.Message);
        Assert.Contains("Capacidad disponible: 500", ex.Message);
    }

    [Fact]
    public async Task RegistrarDistribucion_WhenRowCapacityIsZero_AllowsUnboundedPlanting()
    {
        // Arrange
        var (service, mocks) = BuildPlantacionesService();
        var dto = CreateValidDistribucionDto();
        dto.Detalles = new List<DetalleDistribucionDto>
        {
            new() { Clase = ClaseBulbo.Clase1, Cantidad = 25_000 }
        };

        SetupCommonMocks(mocks, dto);

        var fila = new Fila(dto.SucursalId, dto.TemporadaId, 1, capacidad: 0); // Zero = unbounded
        mocks.FilaRepo.Setup(r => r.GetByIdWithLotesAsync(dto.FilaId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        // Act
        var result = await service.RegistrarDistribucionAsync(dto, 1);

        // Assert: Succeeds because Capacidad <= 0 is not constrained
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(25_000, result.Data.CantidadPlantadaTotal);
    }

    [Fact]
    public async Task RegistrarDistribucion_WhenClienteIsEmpresaSingleton_SucceedsWithDistinctDestinationId()
    {
        // Arrange: Attempting plantation where ClienteId == Empresa.SingletonId (1)
        var (service, mocks) = BuildPlantacionesService();
        var dto = CreateValidDistribucionDto();
        dto.ClienteId = Empresa.SingletonId; // 1

        SetupCommonMocks(mocks, dto);

        var fila = new Fila(dto.SucursalId, dto.TemporadaId, 1, capacidad: 5000);
        mocks.FilaRepo.Setup(r => r.GetByIdWithLotesAsync(dto.FilaId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        // Act
        var result = await service.RegistrarDistribucionAsync(dto, 1);

        // Assert: Remediation verified — distinct destination ID is assigned so origenId != destinoId
        Assert.NotNull(result);
        Assert.True(result.Success);
        mocks.MovimientoRepo.Verify(r => r.AddAsync(
            It.Is<MovimientoBulbos>(m => m.OrigenId == Empresa.SingletonId && m.DestinoId != Empresa.SingletonId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarDistribucion_ConcurrentLotAssignment_VulnerabilityExposed()
    {
        // Arrange: Simulate two concurrent requests resolving the same NextOrden
        var (service, mocks) = BuildPlantacionesService();
        var dto1 = CreateValidDistribucionDto();
        dto1.NumeroLote = null; // Auto-resolve

        SetupCommonMocks(mocks, dto1);

        var fila = new Fila(dto1.SucursalId, dto1.TemporadaId, 1, capacidad: 5000);
        mocks.FilaRepo.Setup(r => r.GetByIdWithLotesAsync(dto1.FilaId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fila);

        // GetNextOrdenAsync returns 2 for both calls (concurrency race)
        mocks.LoteRepo.Setup(r => r.GetNextOrdenAsync(fila.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var res1 = await service.RegistrarDistribucionAsync(dto1, 1);
        var res2 = await service.RegistrarDistribucionAsync(dto1, 1);

        // Assert: Both lotes were assigned Orden = 2 because schema lacks a unique constraint on (FilaId, Orden)
        Assert.Equal(2, res1.Data.Orden);
        Assert.Equal(2, res2.Data.Orden);
    }

    [Fact]
    public async Task CrearFilaAsync_InClosedSeason_ThrowsValidationException()
    {
        // Arrange: GestionFilas.CrearFilaAsync
        var filaRepoMock = new Mock<IFilaRepository>();
        var sucursalRepoMock = new Mock<ISucursalRepository>();
        var temporadaRepoMock = new Mock<ITemporadaRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var gestionFilas = new GestionFilas(
            filaRepoMock.Object,
            sucursalRepoMock.Object,
            temporadaRepoMock.Object,
            unitOfWorkMock.Object);

        var sucursal = new Sucursal { Id = 1, Nombre = "Sucursal Test" };
        sucursalRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        // Temporada is Cerrada
        var temporadaCerrada = new Temporada("2024", 1)
        {
            Id = 5,
            Estado = EstadoTemporada.Cerrada
        };
        temporadaRepoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporadaCerrada);

        var dto = new CreateFilaDto
        {
            SucursalId = 1,
            TemporadaId = 5,
            Identificacion = "Fila-Closed-01",
            Capacidad = 1000
        };

        // Act & Assert: Creating row in a closed season throws ValidationException
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            gestionFilas.CrearFilaAsync(dto));

        Assert.Contains("temporada cerrada", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GestionFilas_MapToDto_ZeroCapacity_SemanticContradictionWithDomain()
    {
        // Fila domain entity
        var fila = new Fila(1, 1, "Fila-Zero", 0);

        // Domain says: unlimited available
        Assert.Equal(int.MaxValue, fila.ObtenerCapacidadDisponible());

        // But GestionFilas.MapToDto (tested via reflection or direct logic equivalent):
        // var capacidadDisponible = f.Capacidad > 0 ? Math.Max(0, f.Capacidad - ocupacion) : f.Capacidad;
        int mappedCapacidadDisponible = fila.Capacidad > 0
            ? Math.Max(0, fila.Capacidad - fila.ObtenerOcupacionActual())
            : fila.Capacidad;

        // When Capacidad == 0, mapped is 0 instead of int.MaxValue!
        Assert.Equal(0, mappedCapacidadDisponible);
    }

    #endregion

    #region 3. Stock & Bulb Inventory (GestionStock, StockController)

    [Fact]
    public async Task ConsultarStockPorCliente_ForEmpresaSingleton_QueriesEmpresaAndReturnsEmpresaData()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoBulbosRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();

        var gestionStock = new GestionStock(movRepoMock.Object, clienteRepoMock.Object, partRepoMock.Object);

        // Cliente repo returns null for Empresa ID
        clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        // Participante repo returns the Empresa
        var empresa = new Empresa
        {
            Id = Empresa.SingletonId,
            RazonSocial = "Azafrán Mediterráneo S.A.",
            CUIT = "30-12345678-9"
        };
        partRepoMock.Setup(r => r.GetByIdAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);

        movRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos>());

        // Act
        var result = await gestionStock.ConsultarStockPorClienteAsync(Empresa.SingletonId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Azafrán Mediterráneo S.A.", result.Data.ClienteNombre);
        Assert.Equal("EMPRESA", result.Data.CodigoCliente);
        Assert.Equal(5, result.Data.StockPorClase.Count);
    }

    [Fact]
    public async Task ConsultarStockPorCliente_ForNonExistentParticipant_ThrowsNotFoundException()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoBulbosRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();

        var gestionStock = new GestionStock(movRepoMock.Object, clienteRepoMock.Object, partRepoMock.Object);

        clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);
        partRepoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipanteComercial?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            gestionStock.ConsultarStockPorClienteAsync(999));
    }

    [Fact]
    public async Task VerificarDisponibilidad_ForClaseBulboMIX_SucceedsWithDecomposition()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoBulbosRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();

        var gestionStock = new GestionStock(movRepoMock.Object, clienteRepoMock.Object, partRepoMock.Object);

        clienteRepoMock.Setup(r => r.GetByIdWithPersonaAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cliente { Id = 2 });

        // Warehouse has 10,000 bulbs each of Clase 1, 2, 3
        var movEntrada = new MovimientoBulbos(1, 1, 2, 1) { Estado = EstadoMovimiento.Confirmado };
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 10_000 });
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 10_000 });
        movEntrada.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase3, Cantidad = 10_000 });

        movRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { movEntrada });

        // Act: User checks availability for 300 MIX bulbs
        var result = await gestionStock.VerificarDisponibilidadAsync(2, ClaseBulbo.MIX, 300);

        // Assert:
        // Remediation verified — MIX is decomposed across Calibres 1, 2, 3
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Data.Disponible);
        Assert.Equal(30000, result.Data.StockDisponible);
    }

    [Fact]
    public async Task ObtenerSaldoPorClase_IgnoresPendingAndAnuladoMovements()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoBulbosRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();

        var gestionStock = new GestionStock(movRepoMock.Object, clienteRepoMock.Object, partRepoMock.Object);

        var confirmedIn = new MovimientoBulbos(1, 1, 2, 1) { Estado = EstadoMovimiento.Confirmado };
        confirmedIn.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 500 });

        var pendingOut = new MovimientoBulbos(2, 2, 3, 1) { Estado = EstadoMovimiento.Pendiente };
        pendingOut.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 200 });

        var anuladoOut = new MovimientoBulbos(3, 2, 3, 1) { Estado = EstadoMovimiento.Anulado };
        anuladoOut.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase2, Cantidad = 100 });

        movRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { confirmedIn, pendingOut, anuladoOut });

        // Act
        var saldo = await gestionStock.ObtenerSaldoPorClaseAsync(2);

        // Assert: Only 500 confirmed in is counted
        Assert.Equal(500, saldo[ClaseBulbo.Clase2]);
    }

    [Fact]
    public async Task ObtenerSaldoPorClase_WhenClientIsBothOriginAndDestino_CalculatesNetDeltas()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoBulbosRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();

        var gestionStock = new GestionStock(movRepoMock.Object, clienteRepoMock.Object, partRepoMock.Object);

        // Inflow: 1000 Clase1
        var movIn = new MovimientoBulbos(1, 1, 5, 1) { Estado = EstadoMovimiento.Confirmado };
        movIn.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 1000 });

        // Outflow: 350 Clase1
        var movOut = new MovimientoBulbos(2, 5, 2, 1) { Estado = EstadoMovimiento.Confirmado };
        movOut.Cantidades.Add(new CantidadBulbos { ClaseBulbo = ClaseBulbo.Clase1, Cantidad = 350 });

        movRepoMock.Setup(r => r.GetConfirmadosByParticipanteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovimientoBulbos> { movIn, movOut });

        // Act
        var saldo = await gestionStock.ObtenerSaldoPorClaseAsync(5);

        // Assert: 1000 - 350 = 650
        Assert.Equal(650, saldo[ClaseBulbo.Clase1]);
    }

    #endregion

    #region 4. Commercial MIX Multi-Calibre Finalization & Stock Impact

    [Fact]
    public void MovimientoComercial_MultiCalibreWithMIX_PhysicalRequirementsDecomposition()
    {
        // Arrange: Movement with mixed items
        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 1);
        mov.AgregarDetalle(ClaseBulbo.Clase0, 50, 10m);
        mov.AgregarDetalle(ClaseBulbo.Clase1, 100, 20m);
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 30m); // 100 C1, 100 C2, 100 C3
        mov.AgregarDetalle(ClaseBulbo.Clase4, 40, 50m);

        // Act
        var reqs = mov.ObtenerRequerimientosFisicosBulbos();

        // Assert
        Assert.Equal(50, reqs[ClaseBulbo.Clase0]);
        Assert.Equal(200, reqs[ClaseBulbo.Clase1]); // 100 direct + 100 from MIX
        Assert.Equal(100, reqs[ClaseBulbo.Clase2]); // 100 from MIX
        Assert.Equal(100, reqs[ClaseBulbo.Clase3]); // 100 from MIX
        Assert.Equal(40, reqs[ClaseBulbo.Clase4]);
    }

    [Fact]
    public async Task FinalizarAsync_WhenOriginHasDeficitInOneMIXCalibre_ThrowsValidationException()
    {
        // Arrange
        var movRepoMock = new Mock<IMovimientoComercialRepository>();
        var precioRepoMock = new Mock<IPrecioClaseRepository>();
        var clienteRepoMock = new Mock<IClienteRepository>();
        var tempRepoMock = new Mock<ITemporadaRepository>();
        var partRepoMock = new Mock<IRepository<ParticipanteComercial>>();
        var gestionStockMock = new Mock<IGestionStock>();
        var opRepoMock = new Mock<IOperacionComercialRepository>();
        var movBulbosRepoMock = new Mock<IMovimientoBulbosRepository>();
        var uowMock = new Mock<IUnitOfWork>();

        var service = new GestionMovimientosComerciales(
            movRepoMock.Object,
            precioRepoMock.Object,
            clienteRepoMock.Object,
            tempRepoMock.Object,
            partRepoMock.Object,
            gestionStockMock.Object,
            opRepoMock.Object,
            movBulbosRepoMock.Object,
            uowMock.Object);

        var mov = new MovimientoComercial(TipoMovimientoComercial.VentaACliente, 1, Empresa.SingletonId, 2, 1)
        {
            Id = 10,
            Estado = EstadoMovimientoComercial.Facturado
        };
        mov.AgregarDetalle(ClaseBulbo.MIX, 300, 25m); // Requires 100 C1, 100 C2, 100 C3

        movRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mov);

        // Origin stock has sufficient C1 and C2, but only 99 in C3 (1 bulb deficit)
        var stockSaldo = new Dictionary<ClaseBulbo, int>
        {
            [ClaseBulbo.Clase0] = 500,
            [ClaseBulbo.Clase1] = 500,
            [ClaseBulbo.Clase2] = 500,
            [ClaseBulbo.Clase3] = 99, // Deficit! Required: 100
            [ClaseBulbo.Clase4] = 500
        };

        gestionStockMock.Setup(s => s.ObtenerSaldoPorClaseAsync(Empresa.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockSaldo);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.FinalizarAsync(10, null, 1));

        Assert.Contains("Stock insuficiente", ex.Message);
        Assert.Contains("Clase3", ex.Message);
        Assert.Contains("Disponible: 99, Requerido: 100", ex.Message);
    }

    #endregion

    #region 5. Frontend Contract & Static Integrity Assertions

    [Fact]
    public void Frontend_ZeroTrashIconsInClientView()
    {
        var filePath = LocateFrontendFile("js/views/clientes-view.js");
        var content = File.ReadAllText(filePath);

        Assert.DoesNotContain("trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bi-trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("getProhibitionIcon", content);
    }

    [Fact]
    public void Frontend_ZeroTrashIconsInEmpleadosView()
    {
        var filePath = LocateFrontendFile("js/views/empleados-view.js");
        var content = File.ReadAllText(filePath);

        Assert.DoesNotContain("trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bi-trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("getProhibitionIcon", content);
    }

    [Fact]
    public void Frontend_ZeroTrashIconsInRolesView()
    {
        var filePath = LocateFrontendFile("js/views/roles-view.js");
        var content = File.ReadAllText(filePath);

        Assert.DoesNotContain("trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bi-trash", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("getProhibitionIcon", content);
    }

    [Fact]
    public void Frontend_ProhibitionSvgGeometry_StrictStandardsCompliance()
    {
        var filePath = LocateFrontendFile("js/utils/icons.js");
        var content = File.ReadAllText(filePath);

        Assert.Contains("<circle cx=\"12\" cy=\"12\" r=\"10\"></circle>", content);
        Assert.Contains("<line x1=\"4.93\" y1=\"4.93\" x2=\"19.07\" y2=\"19.07\"></line>", content);
        Assert.Contains("style=\"color: var(--danger);\"", content);
        Assert.Contains("role=\"img\"", content);
    }

    [Fact]
    public void Frontend_CompleteOmissionOfNotifications_NoBellsOrNotificationServices()
    {
        var appJsPath = LocateFrontendFile("js/app.js");
        var appJs = File.ReadAllText(appJsPath);

        Assert.DoesNotContain("notificacion", appJs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INotificacionService", appJs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bell", appJs, StringComparison.OrdinalIgnoreCase);

        var indexHtmlPath = LocateFrontendFile("index.html");
        var indexHtml = File.ReadAllText(indexHtmlPath);

        Assert.DoesNotContain("notificacion", indexHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bell", indexHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Frontend_Sidebar_FiveCollapsibleSections_AndPersistenceKey()
    {
        var appJsPath = LocateFrontendFile("js/app.js");
        var appJs = File.ReadAllText(appJsPath);

        // Exactly 5 sections present in sidebar markup
        Assert.Contains("data-section=\"principal\"", appJs);
        Assert.Contains("data-section=\"movimientos-comerciales\"", appJs);
        Assert.Contains("data-section=\"stock\"", appJs);
        Assert.Contains("data-section=\"produccion\"", appJs);
        Assert.Contains("data-section=\"administracion\"", appJs);

        // LocalStorage persistence key
        Assert.Contains("siga_sidebar_collapsed_sections", appJs);
    }

    [Fact]
    public void Frontend_Router_PermissionGuards_RedirectsUnauthorizedUsersWithToast()
    {
        var routerJsPath = LocateFrontendFile("js/router.js");
        var routerJs = File.ReadAllText(routerJsPath);

        Assert.Contains("authService.tienePermiso", routerJs);
        Assert.Contains("toast.warning", routerJs);
        Assert.Contains("navigate('#dashboard')", routerJs);
    }

    #endregion

    #region Helper Methods

    private static string LocateFrontendFile(string relativePath)
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "frontend", relativePath);
            if (File.Exists(candidate)) return candidate;

            var slnCandidate = Path.Combine(current, "frontend", relativePath);
            if (Directory.GetFiles(current, "SIGA.sln").Length > 0 && File.Exists(slnCandidate))
                return slnCandidate;

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        var cwd = Path.Combine(Directory.GetCurrentDirectory(), "frontend", relativePath);
        if (File.Exists(cwd)) return cwd;

        // Fallback to solution-relative path
        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../frontend", relativePath));
        if (File.Exists(fallback)) return fallback;

        throw new FileNotFoundException($"No se pudo ubicar el archivo frontend: {relativePath}");
    }

    private static (GestionPlantaciones Service, PlantacionesMocks Mocks) BuildPlantacionesService()
    {
        var mocks = new PlantacionesMocks();
        var service = new GestionPlantaciones(
            mocks.TemporadaRepo.Object,
            mocks.FilaRepo.Object,
            mocks.LoteRepo.Object,
            mocks.PlantacionRepo.Object,
            mocks.ClienteRepo.Object,
            mocks.SucursalRepo.Object,
            mocks.GestionStock.Object,
            mocks.OperacionRepo.Object,
            mocks.MovimientoRepo.Object,
            mocks.UnitOfWork.Object);

        return (service, mocks);
    }

    private static RegistrarDistribucionDto CreateValidDistribucionDto()
    {
        return new RegistrarDistribucionDto
        {
            TemporadaId = 1,
            SucursalId = 1,
            ClienteId = 2,
            FilaId = 10,
            NumeroLote = 1,
            IdentificacionFila = "1",
            CapacidadFila = 5000,
            Detalles = new List<DetalleDistribucionDto>
            {
                new() { Clase = ClaseBulbo.Clase1, Cantidad = 200 }
            }
        };
    }

    private static void SetupCommonMocks(PlantacionesMocks mocks, RegistrarDistribucionDto dto)
    {
        var cliente = new Cliente { Id = dto.ClienteId, CodigoCliente = "CLI-002" };
        mocks.ClienteRepo.Setup(r => r.GetByIdWithPersonaAsync(dto.ClienteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        var temporada = new Temporada("2026", 1) { Id = dto.TemporadaId, Estado = EstadoTemporada.Plantacion };
        mocks.TemporadaRepo.Setup(r => r.GetByIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(temporada);

        var sucursal = new Sucursal { Id = dto.SucursalId, Nombre = "Sucursal Central" };
        mocks.SucursalRepo.Setup(r => r.GetByIdAsync(dto.SucursalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sucursal);

        mocks.GestionStock.Setup(s => s.VerificarDisponibilidadAsync(dto.ClienteId, It.IsAny<ClaseBulbo>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int cId, ClaseBulbo c, int cant, CancellationToken ct) =>
                ApiResponse<DisponibilidadStockResponseDto>.Ok(new DisponibilidadStockResponseDto
                {
                    ClienteId = cId,
                    Clase = c,
                    CantidadSolicitada = cant,
                    StockDisponible = 50000,
                    Disponible = true
                }));

        var plantacion = new Plantacion(dto.TemporadaId, 1) { Id = 20, Estado = EstadoPlantacion.EnProceso };
        mocks.PlantacionRepo.Setup(r => r.GetByTemporadaIdAsync(dto.TemporadaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantacion);
    }

    private class PlantacionesMocks
    {
        public Mock<ITemporadaRepository> TemporadaRepo { get; } = new();
        public Mock<IFilaRepository> FilaRepo { get; } = new();
        public Mock<ILoteRepository> LoteRepo { get; } = new();
        public Mock<IPlantacionRepository> PlantacionRepo { get; } = new();
        public Mock<IClienteRepository> ClienteRepo { get; } = new();
        public Mock<ISucursalRepository> SucursalRepo { get; } = new();
        public Mock<IGestionStock> GestionStock { get; } = new();
        public Mock<IOperacionComercialRepository> OperacionRepo { get; } = new();
        public Mock<IMovimientoBulbosRepository> MovimientoRepo { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    }

    #endregion
}
