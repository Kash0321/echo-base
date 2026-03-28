using EchoBase.Core.BlockedDocks;
using EchoBase.Core.BlockedDocks.Commands;
using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using NSubstitute;

namespace EchoBase.Tests.Unit.BlockedDocks;

public class BlockDocksHandlerTests
{
    private static readonly DateOnly Today = new(2026, 3, 28);
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid Dock1 = Guid.NewGuid();
    private static readonly Guid Dock2 = Guid.NewGuid();

    private readonly IBlockedDockRepository _repository = Substitute.For<IBlockedDockRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly BlockDocksHandler _handler;

    public BlockDocksHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        _repository.UserHasRoleAsync(ManagerId, "Manager", Arg.Any<CancellationToken>()).Returns(true);
        _repository.AllDocksExistAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetActiveBlocksForDocksAsync(
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _handler = new(_repository, _timeProvider);
    }

    private static BlockDocksCommand Cmd(
        Guid? managerId = null,
        IReadOnlyList<Guid>? dockIds = null,
        DateOnly? start = null,
        DateOnly? end = null,
        string reason = "Mantenimiento") =>
        new(managerId ?? ManagerId,
            dockIds ?? [Dock1, Dock2],
            start ?? Today,
            end ?? Today.AddDays(2),
            reason);

    // ──────────────────────────────────────────────────────────────
    // Happy paths
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithIds()
    {
        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, id => Assert.NotEqual(Guid.Empty, id));
        await _repository.Received(1).AddRangeAsync(Arg.Any<IEnumerable<BlockedDock>>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SingleDockSingleDay_Succeeds()
    {
        var result = await _handler.Handle(
            Cmd(dockIds: [Dock1], start: Today, end: Today), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Handle_StartDateToday_Succeeds()
    {
        var result = await _handler.Handle(Cmd(start: Today, end: Today), CancellationToken.None);
        Assert.True(result.IsSuccess);
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

    // ──────────────────────────────────────────────────────────────
    // Input validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyDockList_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(dockIds: []), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.EmptyDockList, result.Error);
    }

    [Fact]
    public async Task Handle_StartDateInPast_ReturnsFailure()
    {
        var result = await _handler.Handle(
            Cmd(start: Today.AddDays(-1), end: Today), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.StartDateInThePast, result.Error);
    }

    [Fact]
    public async Task Handle_EndDateBeforeStartDate_ReturnsFailure()
    {
        var result = await _handler.Handle(
            Cmd(start: Today.AddDays(2), end: Today), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.EndDateBeforeStartDate, result.Error);
    }

    [Fact]
    public async Task Handle_EmptyReason_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(reason: ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.EmptyReason, result.Error);
    }

    [Fact]
    public async Task Handle_WhitespaceReason_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(reason: "   "), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.EmptyReason, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // Dock existence
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DocksNotFound_ReturnsFailure()
    {
        _repository.AllDocksExistAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.DocksNotFound, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // Overlapping blocks
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OverlappingBlocks_ReturnsFailure()
    {
        var existingBlock = new BlockedDock(
            Guid.NewGuid(), Dock1, ManagerId, Today, Today.AddDays(1), "Otro motivo");
        _repository.GetActiveBlocksForDocksAsync(
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([existingBlock]);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BlockedDockErrors.OverlappingBlocks, result.Error);
    }

    [Fact]
    public async Task Handle_AdjacentBlocks_Succeeds()
    {
        // Existing block ends the day before our start → no overlap
        var existingBlock = new BlockedDock(
            Guid.NewGuid(), Dock1, ManagerId, Today.AddDays(-2), Today.AddDays(-1), "Anterior");
        _repository.GetActiveBlocksForDocksAsync(
                Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ──────────────────────────────────────────────────────────────
    // Validation order: role checked before DB queries
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NotManager_DoesNotQueryDocks()
    {
        var basicUser = Guid.NewGuid();
        _repository.UserHasRoleAsync(basicUser, "Manager", Arg.Any<CancellationToken>()).Returns(false);

        await _handler.Handle(Cmd(managerId: basicUser), CancellationToken.None);

        await _repository.DidNotReceive()
            .AllDocksExistAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }
}
