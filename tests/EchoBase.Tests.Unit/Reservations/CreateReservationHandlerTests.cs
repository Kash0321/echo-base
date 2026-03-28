using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Commands;
using NSubstitute;

namespace EchoBase.Tests.Unit.Reservations;

public class CreateReservationHandlerTests
{
    private static readonly DateOnly Today = new(2026, 3, 28);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();

    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CreateReservationHandler _handler;

    public CreateReservationHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        _repository.DockExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetActiveDockReservationsAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetActiveUserReservationsAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _handler = new(_repository, _timeProvider);
    }

    private static CreateReservationCommand Cmd(
        DateOnly? date = null,
        TimeSlot timeSlot = TimeSlot.Morning,
        Guid? userId = null,
        Guid? dockId = null) =>
        new(userId ?? UserId, dockId ?? DockId, date ?? Today, timeSlot);

    // ──────────────────────────────────────────────────────────────
    // Happy paths
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    [InlineData(TimeSlot.Both)]
    public async Task Handle_ValidRequest_ReturnsSuccessWithId(TimeSlot slot)
    {
        var result = await _handler.Handle(Cmd(timeSlot: slot), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _repository.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DateToday_Succeeds()
    {
        var result = await _handler.Handle(Cmd(date: Today), CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_DateExactly7DaysAhead_Succeeds()
    {
        var result = await _handler.Handle(Cmd(date: Today.AddDays(7)), CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    // ──────────────────────────────────────────────────────────────
    // Date validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DateInPast_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(date: Today.AddDays(-1)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateInThePast, result.Error);
    }

    [Fact]
    public async Task Handle_DateTooFarAhead_ReturnsFailure()
    {
        var result = await _handler.Handle(Cmd(date: Today.AddDays(8)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateTooFarAhead, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // Dock existence
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DockNotFound_ReturnsFailure()
    {
        _repository.DockExistsAsync(DockId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotFound, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // Dock availability (time-slot overlaps)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DockAlreadyReservedSameSlot_ReturnsNotAvailable()
    {
        var existing = new Reservation(Guid.NewGuid(), Guid.NewGuid(), DockId, Today, TimeSlot.Morning);
        _repository.GetActiveDockReservationsAsync(DockId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    public async Task Handle_DockHasBoth_AnySlotFails(TimeSlot requested)
    {
        var existing = new Reservation(Guid.NewGuid(), Guid.NewGuid(), DockId, Today, TimeSlot.Both);
        _repository.GetActiveDockReservationsAsync(DockId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await _handler.Handle(Cmd(timeSlot: requested), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    public async Task Handle_DockHasSingleSlot_BothFails(TimeSlot existing)
    {
        var res = new Reservation(Guid.NewGuid(), Guid.NewGuid(), DockId, Today, existing);
        _repository.GetActiveDockReservationsAsync(DockId, Today, Arg.Any<CancellationToken>())
            .Returns([res]);

        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Both), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    [Fact]
    public async Task Handle_DockHasMorning_AfternoonByOtherUserSucceeds()
    {
        var otherUser = Guid.NewGuid();
        var existing = new Reservation(Guid.NewGuid(), otherUser, DockId, Today, TimeSlot.Morning);
        _repository.GetActiveDockReservationsAsync(DockId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Afternoon), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ──────────────────────────────────────────────────────────────
    // User daily limits & slot conflicts
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserHasMorning_BothExceedsMaxSlots()
    {
        var otherDock = Guid.NewGuid();
        var existing = new Reservation(Guid.NewGuid(), UserId, otherDock, Today, TimeSlot.Morning);
        _repository.GetActiveUserReservationsAsync(UserId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        // Morning (1 slot) + Both (2 slots) = 3 > 2
        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Both), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserMaxSlotsExceeded, result.Error);
    }

    [Fact]
    public async Task Handle_UserHasBoth_MorningExceedsMaxSlots()
    {
        var otherDock = Guid.NewGuid();
        var existing = new Reservation(Guid.NewGuid(), UserId, otherDock, Today, TimeSlot.Both);
        _repository.GetActiveUserReservationsAsync(UserId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        // Both (2 slots) + Morning (1 slot) = 3 > 2
        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserMaxSlotsExceeded, result.Error);
    }

    [Fact]
    public async Task Handle_UserHasSameSlotOnOtherDock_ReturnsSlotConflict()
    {
        var otherDock = Guid.NewGuid();
        var existing = new Reservation(Guid.NewGuid(), UserId, otherDock, Today, TimeSlot.Morning);
        _repository.GetActiveUserReservationsAsync(UserId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        // Same Morning slot on a different dock
        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserSlotConflict, result.Error);
    }

    [Fact]
    public async Task Handle_UserHasMorningOnDockA_AfternoonOnDockBSucceeds()
    {
        var dockA = Guid.NewGuid();
        var existing = new Reservation(Guid.NewGuid(), UserId, dockA, Today, TimeSlot.Morning);
        _repository.GetActiveUserReservationsAsync(UserId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var dockB = Guid.NewGuid();
        _repository.DockExistsAsync(dockB, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Cmd(dockId: dockB, timeSlot: TimeSlot.Afternoon), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UserHasMorningOnDock_AfternoonOnSameDockSucceeds()
    {
        var existing = new Reservation(Guid.NewGuid(), UserId, DockId, Today, TimeSlot.Morning);
        _repository.GetActiveUserReservationsAsync(UserId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);
        // Dock also has that Morning reservation (by the same user)
        _repository.GetActiveDockReservationsAsync(DockId, Today, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await _handler.Handle(Cmd(timeSlot: TimeSlot.Afternoon), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ──────────────────────────────────────────────────────────────
    // Unit tests for helper methods
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TimeSlot.Morning, TimeSlot.Morning, true)]
    [InlineData(TimeSlot.Afternoon, TimeSlot.Afternoon, true)]
    [InlineData(TimeSlot.Morning, TimeSlot.Afternoon, false)]
    [InlineData(TimeSlot.Afternoon, TimeSlot.Morning, false)]
    [InlineData(TimeSlot.Both, TimeSlot.Morning, true)]
    [InlineData(TimeSlot.Both, TimeSlot.Afternoon, true)]
    [InlineData(TimeSlot.Morning, TimeSlot.Both, true)]
    [InlineData(TimeSlot.Afternoon, TimeSlot.Both, true)]
    [InlineData(TimeSlot.Both, TimeSlot.Both, true)]
    public void TimeSlotsOverlap_ReturnsExpected(TimeSlot a, TimeSlot b, bool expected)
    {
        Assert.Equal(expected, CreateReservationHandler.TimeSlotsOverlap(a, b));
    }

    [Theory]
    [InlineData(TimeSlot.Morning, 1)]
    [InlineData(TimeSlot.Afternoon, 1)]
    [InlineData(TimeSlot.Both, 2)]
    public void SlotCount_ReturnsExpected(TimeSlot slot, int expected)
    {
        Assert.Equal(expected, CreateReservationHandler.SlotCount(slot));
    }
}
