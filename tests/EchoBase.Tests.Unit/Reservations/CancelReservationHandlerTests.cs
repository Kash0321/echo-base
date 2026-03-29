using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Commands;
using MediatR;
using NSubstitute;

namespace EchoBase.Tests.Unit.Reservations;

public class CancelReservationHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly CancelReservationHandler _handler;

    public CancelReservationHandlerTests()
    {
        _handler = new(_repository, _publisher);
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

    [Fact]
    public async Task Handle_ActiveReservationFutureDate_Succeeds()
    {
        var reservation = MakeActiveReservation();
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ActiveReservationNextDay_Succeeds()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var reservation = MakeActiveReservation(date: tomorrow);
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
    }

    [Fact]
    public async Task Handle_ActiveReservationSameDay_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservation = MakeActiveReservation(date: today);
        _repository.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(Cmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
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
}
