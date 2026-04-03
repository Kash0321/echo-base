using EchoBase.Core.BlockedDocks.Queries;
using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using NSubstitute;

namespace EchoBase.Tests.Unit.BlockedDocks;

public class GetActiveBlocksHandlerTests
{
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid DockId1   = Guid.NewGuid();
    private static readonly Guid DockId2   = Guid.NewGuid();

    private readonly IBlockedDockRepository _repository = Substitute.For<IBlockedDockRepository>();
    private readonly GetActiveBlocksHandler _handler;

    public GetActiveBlocksHandlerTests()
    {
        _handler = new GetActiveBlocksHandler(_repository);
    }

    // ──────────────────────────────────────────────────────────────
    // Lista vacía
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoActiveBlocks_ReturnsEmptyList()
    {
        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    // ──────────────────────────────────────────────────────────────
    // Proyección correcta a DTO
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveBlock_ProjectsToDtoCorrectly()
    {
        var startDate = new DateOnly(2026, 4, 1);
        var endDate   = new DateOnly(2026, 4, 5);
        const string reason = "Mantenimiento eléctrico";

        var block = MakeBlock(DockId1, ManagerId, startDate, endDate, reason,
            dockCode: "N-A01", managerName: "Alice Manager");

        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>()).Returns([block]);

        var result = await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(block.Id, dto.BlockId);
        Assert.Equal(DockId1, dto.DockId);
        Assert.Equal("N-A01", dto.DockCode);
        Assert.Equal(startDate, dto.StartDate);
        Assert.Equal(endDate, dto.EndDate);
        Assert.Equal(reason, dto.Reason);
        Assert.Equal("Alice Manager", dto.BlockedByName);
    }

    // ──────────────────────────────────────────────────────────────
    // Incluye bloqueos cuya fecha ya pasó (no filtra por fecha)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_IncludesPastActiveBlocks()
    {
        var pastBlock = MakeBlock(DockId1, ManagerId,
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 3),
            "Bloqueo pasado");

        var futureBlock = MakeBlock(DockId2, ManagerId,
            new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 5),
            "Bloqueo futuro");

        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>())
            .Returns([pastBlock, futureBlock]);

        var result = await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    // ──────────────────────────────────────────────────────────────
    // Múltiples bloques → proyección de todos
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleBlocks_ReturnsAllProjected()
    {
        var blocks = new List<BlockedDock>
        {
            MakeBlock(DockId1, ManagerId, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 2), "Motivo 1"),
            MakeBlock(DockId2, ManagerId, new DateOnly(2026, 4, 3), new DateOnly(2026, 4, 4), "Motivo 2"),
        };

        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>()).Returns(blocks);

        var result = await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, dto => dto.Reason == "Motivo 1");
        Assert.Contains(result, dto => dto.Reason == "Motivo 2");
    }

    // ──────────────────────────────────────────────────────────────
    // Navegación nula: DockCode y BlockedByName tienen fallback
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NullNavigationProperties_UseFallbackValues()
    {
        // Bloqueo sin propiedades de navegación cargadas
        var block = new BlockedDock(
            Guid.NewGuid(), DockId1, ManagerId,
            new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 2), "Sin nav");
        // Dock y BlockedByUser son null (no se han cargado)

        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>()).Returns([block]);

        var result = await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        Assert.Single(result);
        var dto = result[0];
        // Fallback: DockId.ToString() cuando Dock es null
        Assert.Equal(DockId1.ToString(), dto.DockCode);
        // Fallback: string.Empty cuando BlockedByUser es null
        Assert.Equal(string.Empty, dto.BlockedByName);
    }

    // ──────────────────────────────────────────────────────────────
    // El repositorio es llamado exactamente una vez
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        _repository.GetAllActiveBlocksAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _handler.Handle(new GetActiveBlocksQuery(), CancellationToken.None);

        await _repository.Received(1).GetAllActiveBlocksAsync(Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════

    private static BlockedDock MakeBlock(
        Guid dockId,
        Guid managerId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        string? dockCode = null,
        string? managerName = null)
    {
        var dock = new Dock(dockId)
        {
            Code      = dockCode ?? "N-A01",
            Location  = "Zona de prueba",
            Equipment = "Estándar"
        };

        var manager = new User(managerId)
        {
            Name  = managerName ?? "Manager Dev",
            Email = "manager@test.com"
        };

        var block = new BlockedDock(Guid.NewGuid(), dockId, managerId, startDate, endDate, reason);

        // Establecer propiedades de navegación (private set) simulando lo que hace EF Core
        typeof(BlockedDock).GetProperty("Dock")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(block, [dock]);

        typeof(BlockedDock).GetProperty("BlockedByUser")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(block, [manager]);

        return block;
    }
}
