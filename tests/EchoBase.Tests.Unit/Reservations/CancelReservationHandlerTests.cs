using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Commands;
using NSubstitute;

namespace EchoBase.Tests.Unit.Reservations;

public class CancelReservationHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();
    private static readonly DateOnly FutureDate = new(2026, 3, 30); // 2 days ahead

    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CancelReservationHandler _handler;

    public CancelReservationHandlerTests()
    {
        // Current time: 2026-03-28 10:00 UTC → 38h before FutureDate (March 30 00:00)
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        _handler = new(_repository, _timeProvider);
    }

    private static Reservation MakeActiveReservation(DateOnly? date = null, Guid? userId = null) =>
        new(ReservationId, userId ?? UserId, DockId, date ?? FutureDate, TimeSlot.Morning);

    private static Reservation MakeCancelledReservation(DateOnly? date = null)
    {
        var r = MakeActiveReservation(date);
        r.Cancel();
        return r;
    }

    private CancelReservationCommand Cmd(Guid? reservationId = null, Guid? userId = null) =>
        new(reservationId ?? ReservationId, userId ?? UserId);

    // ──────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidRequest_CancelsAndSaves()
    {
        var reservation = MakeActiveReservation();
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // Validation failures
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReservationNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns((Reservation?)null);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.ReservationNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsFailure()
    {
        var reservation = MakeActiveReservation();
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        var otherUser = Guid.NewGuid();

        var result = await _handler.Handle(Cmd(userId: otherUser), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.NotReservationOwner, result.Error);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFailure()
    {
        var reservation = MakeCancelledReservation();
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.AlreadyCancelled, result.Error);
    }

    // ──────────────────────────────────────────────────────────────
    // 24-hour cancellation window
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_LessThan24HoursBeforeDate_ReturnsCancellationTooLate()
    {
        // Reservation for March 29, current time is March 28 10:00 → 14h left → too late
        var tomorrow = new DateOnly(2026, 3, 29);
        var reservation = MakeActiveReservation(date: tomorrow);
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.CancellationTooLate, result.Error);
    }

    [Fact]
    public async Task Handle_Exactly24HoursBeforeDate_Succeeds()
    {
        // March 28 00:00 UTC → March 29 00:00 UTC = exactly 24h → >= 24h → allowed
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero));
        var reservation = MakeActiveReservation(date: new DateOnly(2026, 3, 29));
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_MoreThan24HoursBeforeDate_Succeeds()
    {
        // Default: March 28 10:00 → March 30 00:00 = 38h → OK
        var reservation = MakeActiveReservation();
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_OneSecondBefore24Hours_ReturnsCancellationTooLate()
    {
        // March 28 00:00:01 UTC → March 29 00:00 UTC = 23h 59m 59s → too late
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 0, 0, 1, TimeSpan.Zero));
        var reservation = MakeActiveReservation(date: new DateOnly(2026, 3, 29));
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.CancellationTooLate, result.Error);
    }
}
