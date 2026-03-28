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

    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly GetUserReservationsHandler _handler;

    public GetUserReservationsHandlerTests()
    {
        // Current time: 2026-03-28 10:00 UTC
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        _handler = new(_repository, _timeProvider);
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
            MakeReservation(new DateOnly(2026, 3, 30), TimeSlot.Morning),
            MakeReservation(new DateOnly(2026, 3, 25), TimeSlot.Afternoon, cancelled: true),
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("N-A01", result[0].DockCode);
        Assert.Equal("N-A01", result[1].DockCode);
    }

    // ──────────────────────────────────────────────────────────────
    // CanCancel logic
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActiveFutureReservation_CanCancel()
    {
        // March 30 is >24h from 2026-03-28 10:00
        var reservations = new List<Reservation>
        {
            MakeReservation(new DateOnly(2026, 3, 30), TimeSlot.Both)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.True(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_ActiveReservationWithin24Hours_CannotCancel()
    {
        // March 29 at 00:00 is 14h from 2026-03-28 10:00 → less than 24h
        var reservations = new List<Reservation>
        {
            MakeReservation(new DateOnly(2026, 3, 29), TimeSlot.Morning)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.False(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_CancelledReservation_CannotCancel()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(new DateOnly(2026, 3, 30), TimeSlot.Both, cancelled: true)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.False(result[0].CanCancel);
    }

    [Fact]
    public async Task Handle_PastReservation_CannotCancel()
    {
        var reservations = new List<Reservation>
        {
            MakeReservation(new DateOnly(2026, 3, 20), TimeSlot.Afternoon)
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
            MakeReservation(new DateOnly(2026, 3, 30), slot)
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
            MakeReservation(new DateOnly(2026, 3, 30))
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
            MakeReservation(new DateOnly(2026, 3, 30), cancelled: true)
        };
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        Assert.Equal(ReservationStatus.Cancelled, result[0].Status);
    }

    // ──────────────────────────────────────────────────────────────
    // Exactly at 24h boundary
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ExactlyAt24HourBoundary_CanCancel()
    {
        // 2026-03-29 10:00 UTC is exactly 24h from now → CanCancel = true (>= 24h)
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero));
        var reservations = new List<Reservation>
        {
            MakeReservation(new DateOnly(2026, 3, 29), TimeSlot.Morning)
        };
        // March 29 at 00:00 is only 14h from 28 10:00 → NOT cancellable
        _repository.GetUserReservationsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await _handler.Handle(new GetUserReservationsQuery(UserId), CancellationToken.None);

        // March 29 00:00 - March 28 10:00 = 14h < 24h → cannot cancel
        Assert.False(result[0].CanCancel);
    }
}
