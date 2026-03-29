using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Queries;
using NSubstitute;

namespace EchoBase.Tests.Unit.Reservations;

public class GetUserReservationsHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DockId1 = Guid.NewGuid();
    private static readonly Guid DockId2 = Guid.NewGuid();
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
    private static readonly DateOnly PastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);

    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly GetUserReservationsHandler _handler;

    public GetUserReservationsHandlerTests()
    {
        _handler = new(_repository);
    }

    private static Dock MakeDock(Guid id, string code) =>
        new(id) { Code = code, Location = "Test", Equipment = "Monitor" };

    private static Reservation MakeReservation(
        DateOnly date,
        TimeSlot slot = TimeSlot.Morning,
        Guid? dockId = null,
        bool cancelled = false)
    {
        var r = new Reservation(Guid.NewGuid(), UserId, dockId ?? DockId1, date, slot);
        var dock = MakeDock(dockId ?? DockId1, dockId == DockId2 ? "D-1A01" : "N-A01");
        r.SetDock(dock);
        if (cancelled) r.Cancel();
        return r;
    }

    // ──────────────────────────────────────────────────────────────
    // Empty history
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReservations_ReturnsEmptyList()
    {
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Empty(result);
    }

    // ──────────────────────────────────────────────────────────────
    // Returns all reservations with dock codes
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleReservations_ReturnsAll()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate, TimeSlot.Morning),
            MakeReservation(PastDate, TimeSlot.Afternoon, cancelled: true),
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("N-A01", result[0].DockCode);
        Assert.Equal("N-A01", result[1].DockCode);
    }

    // ──────────────────────────────────────────────────────────────
    // CanCancel logic — cualquier reserva activa es cancelable
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveFutureReservation_CanCancel()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate, TimeSlot.Both)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.True(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_ActiveReservationSameDay_CanCancel()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservations = new List<Reservation>
        {
            MakeReservation(today, TimeSlot.Morning)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.True(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_ActiveReservationNextDay_CanCancel()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var reservations = new List<Reservation>
        {
            MakeReservation(tomorrow, TimeSlot.Morning)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.True(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_CancelledReservation_CannotCancel()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate, TimeSlot.Both, cancelled: true)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.False(result[0].CanCancel);
    }

    // ──────────────────────────────────────────────────────────────
    // Slot and status mapping
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    [InlineData(TimeSlot.Both)]
    public async Task Handle_PreservesTimeSlot(TimeSlot slot)
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate, slot)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(slot, result[0].TimeSlot);
    }

    [Fact]
    public async Task Handle_ActiveReservation_HasActiveStatus()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(ReservationStatus.Active, result[0].Status);
    }

    [Fact]
    public async Task Handle_CancelledReservation_HasCancelledStatus()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(FutureDate, cancelled: true)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(ReservationStatus.Cancelled, result[0].Status);
    }
}
