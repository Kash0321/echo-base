using EchoBase.Core.BlockedDocks;
using EchoBase.Core.BlockedDocks.Commands;
using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using NSubstitute;

namespace EchoBase.Tests.Unit.BlockedDocks;

public class UnblockDocksHandlerTests
{
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid Block1Id = Guid.NewGuid();
    private static readonly Guid Block2Id = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();

    private readonly IBlockedDockRepository _repository = Substitute.For<IBlockedDockRepository>();
    private readonly UnblockDocksHandler _handler;

    public UnblockDocksHandlerTests()
    {
        _repository.UserHasRoleAsync(ManagerId, "Manager", Arg.Any<CancellationToken>()).Returns(true);
        _handler = new(_repository);
    }

    private static BlockedDock MakeActiveBlock(Guid? id = null) =>
        new(id ?? Block1Id, DockId, ManagerId,
            new DateOnly(2026, 3, 28), new DateOnly(2026, 3, 30), "Mantenimiento");

    private static BlockedDock MakeDeactivatedBlock(Guid? id = null)
    {
        var block = MakeActiveBlock(id);
        block.Deactivate();
        return block;
    }

    private static UnblockDocksCommand Cmd(
        Guid? managerId = null,
        IReadOnlyList<Guid>? blockIds = null) =>
        new(managerId ?? ManagerId, blockIds ?? [Block1Id]);

    // ──────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidSingleBlock_DeactivatesAndSaves()
    {
        var block = MakeActiveBlock();
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([block]);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(block.IsActive);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleBlocks_DeactivatesAll()
    {
        var block1 = MakeActiveBlock(Block1Id);
        var block2 = MakeActiveBlock(Block2Id);
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([block1, block2]);

        var result = await _handler.Handle(
            Cmd(blockIds: [Block1Id, Block2Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(block1.IsActive);
        Assert.False(block2.IsActive);
    }

    // ──────────────────────────────────────────────────────────────
    // Role validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NotManager_ReturnsFailure()
    {
        var basicUser = Guid.NewGuid();
        _repository.UserHasRoleAsync(basicUser, "Manager", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(Cmd(managerId: basicUser), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.NotManager, result.Error);
    }

    [Fact]
    public async Task Handle_NotManager_DoesNotQueryBlocks()
    {
        var basicUser = Guid.NewGuid();
        _repository.UserHasRoleAsync(basicUser, "Manager", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(Cmd(managerId: basicUser), CancellationToken.None);

        await _repository.DidNotReceive()
            .GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // Input validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyBlockList_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(blockIds: []), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.EmptyDockList, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // Block existence / state
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BlockNotFound_ReturnsFailure()
    {
        // Requested 1, returned 0
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.BlocksNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_SomeBlocksNotFound_ReturnsFailure()
    {
        // Requested 2, returned 1
        var block1 = MakeActiveBlock(Block1Id);
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([block1]);

        var result = await _handler.Handle(
            Cmd(blockIds: [Block1Id, Block2Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.BlocksNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_AlreadyDeactivated_ReturnsFailure()
    {
        var deactivated = MakeDeactivatedBlock();
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([deactivated]);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.AlreadyDeactivated, result.Error);
    }

    [Fact]
    public async Task Handle_MixedActiveAndDeactivated_ReturnsFailure()
    {
        var active = MakeActiveBlock(Block1Id);
        var deactivated = MakeDeactivatedBlock(Block2Id);
        _repository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([active, deactivated]);

        var result = await _handler.Handle(
            Cmd(blockIds: [Block1Id, Block2Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.AlreadyDeactivated, result.Error);
        // El activo no debería haberse desactivado
        Assert.True(active.IsActive);
    }
}
