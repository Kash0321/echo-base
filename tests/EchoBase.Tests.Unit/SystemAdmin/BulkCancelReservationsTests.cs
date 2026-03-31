using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using MediatR;
using NSubstitute;

namespace EchoBase.Tests.Unit.SystemAdmin;

public class BulkCancelReservationsTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 4, 1);
    private static readonly DateOnly End = new(2026, 4, 5);

    private readonly IBlockedDockRepository _blockedDockRepo = Substitute.For<IBlockedDockRepository>();
    private readonly IReservationRepository _reservationRepo = Substitute.For<IReservationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly BulkCancelReservationsHandler _handler;

    public BulkCancelReservationsTests()
    {
        _handler = new(_blockedDockRepo, _reservationRepo, _publisher);
    }

    private static BulkCancelReservationsCommand Cmd(
        DateOnly? start = null,
        DateOnly? end = null,
        IReadOnlyList<Guid>? dockIds = null) =>
        new(AdminId, start ?? Start, end ?? End, "Cierre de emergencia", dockIds);

    private static Reservation BuildReservation(DateOnly? date = null)
    {
        var dockId = Guid.NewGuid();
        var dock = new Dock(dockId) { Code = "N-A01", Location = "N", Equipment = "" };
        var res = new Reservation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            dockId,
            date ?? Start,
            TimeSlot.Morning);
        // Set navigation property via reflection for test purposes
        typeof(Reservation)
            .GetProperty(nameof(Reservation.Dock))!
            .SetValue(res, dock);
        return res;
    }

    // ── Happy paths ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithActiveReservations_CancelsAllAndPublishesNotifications()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var reservations = new List<Reservation> { BuildReservation(), BuildReservation() };
        _reservationRepo.GetActiveReservationsInRangeAsync(Start, End, null, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CancelledCount);
        Assert.All(reservations, r => Assert.Equal(ReservationStatus.Cancelled, r.Status));
        await _publisher.Received(2).Publish(
            Arg.Any<ReservationCancelledNotification>(), Arg.Any<CancellationToken>());
        await _reservationRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoActiveReservations_ReturnsZeroCount()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        _reservationRepo.GetActiveReservationsInRangeAsync(Start, End, null, Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.CancelledCount);
        await _reservationRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDockIdsFilter_PassesFilterToRepository()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        var dockIds = new List<Guid> { Guid.NewGuid() };
        _reservationRepo.GetActiveReservationsInRangeAsync(Start, End, dockIds, Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        await _handler.Handle(Cmd(dockIds: dockIds), CancellationToken.None);

        await _reservationRepo.Received(1).GetActiveReservationsInRangeAsync(
            Start, End, dockIds, Arg.Any<CancellationToken>());
    }

    // ── Validaciones ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task Handle_EndDateBeforeStartDate_ReturnsInvalidDateRange()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Cmd(start: End, end: Start), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.InvalidDateRange, result.Error);
    }

    [Fact]
    public async Task Handle_SameDateRange_Succeeds()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
        _reservationRepo.GetActiveReservationsInRangeAsync(Start, Start, null, Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        var result = await _handler.Handle(Cmd(start: Start, end: Start), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
