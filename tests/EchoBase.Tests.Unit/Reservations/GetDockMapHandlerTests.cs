using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Queries;
using NSubstitute;

namespace EchoBase.Tests.Unit.Reservations;

public class GetDockMapHandlerTests
{
    private static readonly Guid NostromoZoneId = Guid.NewGuid();
    private static readonly Guid DerelictZoneId = Guid.NewGuid();

    private readonly IDockMapRepository _repository = Substitute.For<IDockMapRepository>();
    private readonly GetDockMapHandler _handler;

    public GetDockMapHandlerTests()
    {
        _repository.GetAllZonesWithDocksAsync(Arg.Any<CancellationToken>())
            .Returns(BuildZones());
        _repository.GetAllActiveReservationsForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _repository.GetBlockedDockIdsForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new(_repository);
    }

    private static readonly DateOnly TestDate = new(2026, 3, 28);
    private static GetDockMapQuery Query(DateOnly? date = null) => new(date ?? TestDate);

    // ─── Dock IDs ────────────────────────────────────────────

    private static readonly Guid DockNA01 = Guid.NewGuid();
    private static readonly Guid DockNA02 = Guid.NewGuid();
    private static readonly Guid DockNB01 = Guid.NewGuid();
    private static readonly Guid DockNB02 = Guid.NewGuid();
    private static readonly Guid DockD1A01 = Guid.NewGuid();
    private static readonly Guid DockD1B01 = Guid.NewGuid();
    private static readonly Guid DockD2A01 = Guid.NewGuid();
    private static readonly Guid DockD2B01 = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────
    // Structure tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsCorrectDate()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(TestDate, result.Date);
    }

    [Fact]
    public async Task Handle_ReturnsTwoZonesOrderedDescendingByName()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(2, result.Zones.Count);
        Assert.Equal("Nostromo", result.Zones[0].Name);
        Assert.Equal("Derelict", result.Zones[1].Name);
    }

    [Fact]
    public async Task Handle_NostromoHasOneTable()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        var nostromo = result.Zones.Single(z => z.Name == "Nostromo");
        Assert.Single(nostromo.Tables);
        Assert.Equal("Nostromo", nostromo.Tables[0].Name);
    }

    [Fact]
    public async Task Handle_DerelictHasTwoTables()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        var derelict = result.Zones.Single(z => z.Name == "Derelict");
        Assert.Equal(2, derelict.Tables.Count);
        Assert.Equal("Mesa 1", derelict.Tables[0].Name);
        Assert.Equal("Mesa 2", derelict.Tables[1].Name);
    }

    [Fact]
    public async Task Handle_NostromoTable_HasCorrectSides()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        var table = result.Zones.Single(z => z.Name == "Nostromo").Tables[0];
        Assert.Equal(2, table.SideA.Count);
        Assert.Equal(2, table.SideB.Count);
        Assert.Equal("N-A01", table.SideA[0].Code);
        Assert.Equal("N-B01", table.SideB[0].Code);
    }

    [Fact]
    public async Task Handle_DerelictTable1_HasCorrectSides()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        var table = result.Zones.Single(z => z.Name == "Derelict").Tables[0];
        Assert.Single(table.SideA);
        Assert.Single(table.SideB);
        Assert.Equal("D-1A01", table.SideA[0].Code);
        Assert.Equal("D-1B01", table.SideB[0].Code);
    }

    // ──────────────────────────────────────────────────────────
    // Status: Free
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoReservationsNoBlocks_AllDocksAreFree()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        var allSeats = result.Zones.SelectMany(z => z.Tables)
            .SelectMany(t => t.SideA.Concat(t.SideB));

        Assert.All(allSeats, s =>
        {
            Assert.Equal(DockStatus.Free, s.Status);
            Assert.Null(s.BookedSlot);
        });
    }

    // ──────────────────────────────────────────────────────────
    // Status: Blocked
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DockBlocked_StatusIsBlocked()
    {
        _repository.GetBlockedDockIdsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([DockNA01]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.Blocked, seat.Status);
        Assert.Null(seat.BookedSlot);
    }

    [Fact]
    public async Task Handle_DockBlockedOverridesReservation()
    {
        // Aunque haya reserva, si está bloqueado, el estado es Blocked
        _repository.GetBlockedDockIdsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([DockNA01]);
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Morning)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(DockStatus.Blocked, FindSeat(result, DockNA01).Status);
    }

    // ──────────────────────────────────────────────────────────
    // Status: PartiallyBooked
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TimeSlot.Morning)]
    [InlineData(TimeSlot.Afternoon)]
    public async Task Handle_OneSlotBooked_StatusIsPartiallyBooked(TimeSlot bookedSlot)
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, bookedSlot)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.PartiallyBooked, seat.Status);
        Assert.Equal(bookedSlot, seat.BookedSlot);
    }

    // ──────────────────────────────────────────────────────────
    // Status: FullyBooked
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BothSlotsBooked_StatusIsFullyBooked()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([
                MakeReservation(DockNA01, TimeSlot.Morning),
                MakeReservation(DockNA01, TimeSlot.Afternoon),
            ]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.FullyBooked, seat.Status);
        Assert.Null(seat.BookedSlot);
    }

    [Fact]
    public async Task Handle_BothSlotBooked_StatusIsFullyBooked()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Both)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(DockStatus.FullyBooked, FindSeat(result, DockNA01).Status);
    }

    // ──────────────────────────────────────────────────────────
    // Mixed statuses
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MixedStatuses_EachDockHasCorrectStatus()
    {
        _repository.GetBlockedDockIdsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([DockNB01]);
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([
                MakeReservation(DockNA01, TimeSlot.Morning),
                MakeReservation(DockNA02, TimeSlot.Both),
            ]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(DockStatus.PartiallyBooked, FindSeat(result, DockNA01).Status);
        Assert.Equal(DockStatus.FullyBooked, FindSeat(result, DockNA02).Status);
        Assert.Equal(DockStatus.Blocked, FindSeat(result, DockNB01).Status);
        Assert.Equal(DockStatus.Free, FindSeat(result, DockNB02).Status);
    }

    // ──────────────────────────────────────────────────────────
    // ParseTableKey tests
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("N-A01", "N")]
    [InlineData("N-B06", "N")]
    [InlineData("D-1A01", "D-1")]
    [InlineData("D-1B03", "D-1")]
    [InlineData("D-2A01", "D-2")]
    [InlineData("D-2B03", "D-2")]
    public void ParseTableKey_ReturnsExpectedKey(string code, string expected) =>
        Assert.Equal(expected, GetDockMapHandler.ParseTableKey(code));

    [Theory]
    [InlineData("N-A01", "A")]
    [InlineData("N-B06", "B")]
    [InlineData("D-1A01", "A")]
    [InlineData("D-1B03", "B")]
    [InlineData("D-2A01", "A")]
    [InlineData("D-2B03", "B")]
    public void ParseSide_ReturnsExpectedSide(string code, string expected) =>
        Assert.Equal(expected, GetDockMapHandler.ParseSide(code));

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private static DockSeatDto FindSeat(DockMapDto map, Guid dockId) =>
        map.Zones.SelectMany(z => z.Tables)
            .SelectMany(t => t.SideA.Concat(t.SideB))
            .Single(s => s.Id == dockId);

    private static Reservation MakeReservation(Guid dockId, TimeSlot slot) =>
        new(Guid.NewGuid(), Guid.NewGuid(), dockId, TestDate, slot);

    private static List<DockZone> BuildZones()
    {
        var nostromo = new DockZone(NostromoZoneId) { Name = "Nostromo", Description = "6+6" };
        var derelict = new DockZone(DerelictZoneId) { Name = "Derelict", Description = "3+3 · 3+3" };

        AddDock(nostromo, DockNA01, "N-A01");
        AddDock(nostromo, DockNA02, "N-A02");
        AddDock(nostromo, DockNB01, "N-B01");
        AddDock(nostromo, DockNB02, "N-B02");

        AddDock(derelict, DockD1A01, "D-1A01");
        AddDock(derelict, DockD1B01, "D-1B01");
        AddDock(derelict, DockD2A01, "D-2A01");
        AddDock(derelict, DockD2B01, "D-2B01");

        return [nostromo, derelict];
    }

    private static void AddDock(DockZone zone, Guid id, string code)
    {
        var dock = new Dock(id)
        {
            Code = code,
            Location = $"Test - {code}",
            Equipment = "Monitor doble"
        };
        dock.AssignToZone(zone);
        zone.Docks.Add(dock);
    }
}
