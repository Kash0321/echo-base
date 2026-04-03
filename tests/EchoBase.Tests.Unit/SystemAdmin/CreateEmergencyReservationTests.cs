using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using MediatR;
using NSubstitute;

namespace EchoBase.Tests.Unit.SystemAdmin;

public class CreateEmergencyReservationTests
{
    private static readonly DateOnly Today = new(2026, 3, 28);
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();
    private static readonly Guid DockNA = Guid.NewGuid();
    private static readonly Guid DockNB = Guid.NewGuid();

    private readonly IBlockedDockRepository _blockedDockRepo = Substitute.For<IBlockedDockRepository>();
    private readonly IReservationRepository _reservationRepo = Substitute.For<IReservationRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly CreateEmergencyReservationHandler _handler;

    public CreateEmergencyReservationTests()
    {
        _time.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));

        // Defaults: admin is SystemAdmin, dock exists and is free, user has no reservations
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        _reservationRepo.DockExistsAsync(DockId, Arg.Any<CancellationToken>()).Returns(true);
        _blockedDockRepo.IsDockBlockedAsync(DockId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(false);
        _reservationRepo.GetActiveDockReservationsAsync(DockId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());
        _reservationRepo.GetActiveUserReservationsAsync(TargetUserId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());
        _reservationRepo.GetDockCodeAsync(DockId, Arg.Any<CancellationToken>()).Returns("N-A01");

        _handler = new(_blockedDockRepo, _reservationRepo, _userRepo, _publisher, _time);
    }

    private CreateEmergencyReservationCommand Cmd(
        DateOnly? date = null,
        TimeSlot slot = TimeSlot.Morning,
        Guid? dockId = null,
        Guid? targetUserId = null) =>
        new(AdminId, targetUserId ?? TargetUserId, dockId ?? DockId, date ?? Today, slot);

    // ── Happy paths ───────────────────────────────────────────────

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    [InlineData(TimeSlot.Both)]
    public async Task Handle_ValidRequest_ReturnsSuccessWithId(TimeSlot slot)
    {
        var result = await _handler.Handle(Cmd(slot: slot), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _reservationRepo.Received(1).AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>());
        await _reservationRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Valid_PublishesCreatedNotificationToTargetUser()
    {
        await _handler.Handle(Cmd(), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ReservationCreatedNotification>(n => n.UserId == TargetUserId),
            Arg.Any<CancellationToken>());
    }

    // ── Authorization ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ── Date validations (same as normal reservation) ─────────────

    [Fact]
    public async Task Handle_DateInPast_ReturnsDateInPastError()
    {
        var result = await _handler.Handle(Cmd(date: Today.AddDays(-1)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateInThePast, result.Error);
    }

    [Fact]
    public async Task Handle_DateTooFarAhead_ReturnsTooFarAheadError()
    {
        var result = await _handler.Handle(Cmd(date: Today.AddDays(8)), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateTooFarAhead, result.Error);
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

    // ── Dock validations ──────────────────────────────────────────

    [Fact]
    public async Task Handle_DockNotFound_ReturnsDockNotFoundError()
    {
        _reservationRepo.DockExistsAsync(DockId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_DockBlocked_ReturnsDockBlockedError()
    {
        _blockedDockRepo.IsDockBlockedAsync(DockId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockBlocked, result.Error);
    }

    [Fact]
    public async Task Handle_DockFullyBooked_ReturnsDockNotAvailableError()
    {
        var existing = new Reservation(Guid.NewGuid(), Guid.NewGuid(), DockId, Today, TimeSlot.Both);
        _reservationRepo.GetActiveDockReservationsAsync(DockId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation> { existing });

        var result = await _handler.Handle(Cmd(slot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    // ── User validations (applied to target user) ─────────────────

    [Fact]
    public async Task Handle_TargetUserMaxSlotsExceeded_ReturnsError()
    {
        var existingReservations = new List<Reservation>
        {
            new(Guid.NewGuid(), TargetUserId, DockNA, Today, TimeSlot.Morning),
            new(Guid.NewGuid(), TargetUserId, DockNB, Today, TimeSlot.Afternoon)
        };
        _reservationRepo.GetActiveUserReservationsAsync(TargetUserId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(existingReservations);

        var result = await _handler.Handle(Cmd(slot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserMaxSlotsExceeded, result.Error);
    }

    [Fact]
    public async Task Handle_TargetUserSlotConflict_ReturnsError()
    {
        var existingReservations = new List<Reservation>
        {
            new(Guid.NewGuid(), TargetUserId, DockNA, Today, TimeSlot.Morning)
        };
        _reservationRepo.GetActiveUserReservationsAsync(TargetUserId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(existingReservations);

        var result = await _handler.Handle(Cmd(slot: TimeSlot.Morning), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserSlotConflict, result.Error);
    }
}
