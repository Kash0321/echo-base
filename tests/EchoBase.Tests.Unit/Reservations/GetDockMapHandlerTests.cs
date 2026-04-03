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
        _repository.GetBlockedDocksForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
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
    public async Task Handle_ReturnsTwoZonesOrderedByOrder()
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
        _repository.GetBlockedDocksForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeBlock(DockNA01)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.Blocked, seat.Status);
        Assert.Null(seat.BookedSlot);
    }

    [Fact]
    public async Task Handle_DockBlockedOverridesReservation()
    {
        // Aunque haya reserva, si está bloqueado, el estado es Blocked
        _repository.GetBlockedDocksForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeBlock(DockNA01)]);
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Morning)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(DockStatus.Blocked, FindSeat(result, DockNA01).Status);
    }

    [Fact]
    public async Task Handle_DockBlocked_BlockInfoPropagatedToDto()
    {
        var block = MakeBlock(DockNA01, blockedByName: "Han Solo", reason: "Mantenimiento eléctrico");
        _repository.GetBlockedDocksForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([block]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal("Han Solo", seat.BlockedByName);
        Assert.Equal("Mantenimiento eléctrico", seat.BlockReason);
    }

    [Fact]
    public async Task Handle_DockBlocked_BlockInfoNullWhenUserNotLoaded()
    {
        var block = MakeBlock(DockNA01); // sin usuario cargado
        _repository.GetBlockedDocksForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([block]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.Blocked, seat.Status);
        Assert.Null(seat.BlockedByName);
        Assert.Equal("Test reason", seat.BlockReason);
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
        _repository.GetBlockedDocksForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeBlock(DockNB01)]);
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
    // User names in reservations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_MorningReservation_MorningBookedByPopulated()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Morning, userName: "Luke Skywalker")]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal("Luke Skywalker", seat.MorningBookedBy);
        Assert.Null(seat.AfternoonBookedBy);
    }

    [Fact]
    public async Task Handle_AfternoonReservation_AfternoonBookedByPopulated()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Afternoon, userName: "Leia Organa")]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Null(seat.MorningBookedBy);
        Assert.Equal("Leia Organa", seat.AfternoonBookedBy);
    }

    [Fact]
    public async Task Handle_BothSlotReservation_BothNamesPopulatedWithSameUser()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Both, userName: "Han Solo")]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal("Han Solo", seat.MorningBookedBy);
        Assert.Equal("Han Solo", seat.AfternoonBookedBy);
    }

    [Fact]
    public async Task Handle_TwoReservations_EachSlotHasCorrectUserName()
    {
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([
                MakeReservation(DockNA01, TimeSlot.Morning,   userName: "R2-D2"),
                MakeReservation(DockNA01, TimeSlot.Afternoon, userName: "C-3PO"),
            ]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.FullyBooked, seat.Status);
        Assert.Equal("R2-D2", seat.MorningBookedBy);
        Assert.Equal("C-3PO", seat.AfternoonBookedBy);
    }

    [Fact]
    public async Task Handle_ReservationWithoutUser_NamesAreNull()
    {
        // Simula que el repositorio no cargó la nav. property User
        _repository.GetAllActiveReservationsForDateAsync(TestDate, Arg.Any<CancellationToken>())
            .Returns([MakeReservation(DockNA01, TimeSlot.Morning)]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        var seat = FindSeat(result, DockNA01);
        Assert.Equal(DockStatus.PartiallyBooked, seat.Status);
        Assert.Null(seat.MorningBookedBy);
        Assert.Null(seat.AfternoonBookedBy);
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────

    private static DockSeatDto FindSeat(DockMapDto map, Guid dockId) =>
        map.Zones.SelectMany(z => z.Tables)
            .SelectMany(t => t.SideA.Concat(t.SideB))
            .Single(s => s.Id == dockId);

    private static Reservation MakeReservation(Guid dockId, TimeSlot slot, string? userName = null)
    {
        var reservation = new Reservation(Guid.NewGuid(), Guid.NewGuid(), dockId, TestDate, slot);
        if (userName is not null)
        {
            var user = new User(Guid.NewGuid()) { Name = userName, Email = $"{userName.ToLower().Replace(' ', '.')}@test.com" };
            reservation.SetUser(user);
        }
        return reservation;
    }

    private static BlockedDock MakeBlock(Guid dockId, string? blockedByName = null, string reason = "Test reason")
    {
        var block = new BlockedDock(Guid.NewGuid(), dockId, Guid.NewGuid(), TestDate, TestDate, reason);
        if (blockedByName is not null)
        {
            var manager = new User(Guid.NewGuid()) { Name = blockedByName, Email = $"{blockedByName.ToLower().Replace(' ', '.')}@test.com" };
            block.SetBlockedByUser(manager);
        }
        return block;
    }

    private static List<DockZone> BuildZones()
    {
        var nostromo = new DockZone(NostromoZoneId) { Name = "Nostromo", Description = "6+6", Order = 0 };
        var derelict = new DockZone(DerelictZoneId) { Name = "Derelict", Description = "3+3 · 3+3", Order = 1 };

        var nostromoTable = new DockTable(Guid.NewGuid()) { TableKey = "N" };
        nostromoTable.AssignToZone(nostromo);
        AddDockToTable(nostromoTable, DockNA01,  "N-A01",  DockSide.A);
        AddDockToTable(nostromoTable, DockNA02,  "N-A02",  DockSide.A);
        AddDockToTable(nostromoTable, DockNB01,  "N-B01",  DockSide.B);
        AddDockToTable(nostromoTable, DockNB02,  "N-B02",  DockSide.B);
        ((List<DockTable>)nostromo.Tables).Add(nostromoTable);

        var derelictTable1 = new DockTable(Guid.NewGuid()) { TableKey = "D-1" };
        derelictTable1.AssignToZone(derelict);
        AddDockToTable(derelictTable1, DockD1A01, "D-1A01", DockSide.A);
        AddDockToTable(derelictTable1, DockD1B01, "D-1B01", DockSide.B);
        ((List<DockTable>)derelict.Tables).Add(derelictTable1);

        var derelictTable2 = new DockTable(Guid.NewGuid()) { TableKey = "D-2" };
        derelictTable2.AssignToZone(derelict);
        AddDockToTable(derelictTable2, DockD2A01, "D-2A01", DockSide.A);
        AddDockToTable(derelictTable2, DockD2B01, "D-2B01", DockSide.B);
        ((List<DockTable>)derelict.Tables).Add(derelictTable2);

        return [nostromo, derelict];
    }

    private static void AddDockToTable(DockTable table, Guid id, string code, DockSide side)
    {
        var dock = new Dock(id)
        {
            Code = code,
            Location = $"Test - {code}",
            Equipment = "Monitor doble"
        };
        dock.AssignToTable(table, side);
        ((List<Dock>)table.Docks).Add(dock);
    }

    // ──────────────────────────────────────────────────────────
    // Orientation tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ZoneOrientation_IsPropagedToDtoAsHorizontalByDefault()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.All(result.Zones, z =>
            Assert.Equal(ZoneOrientation.Horizontal, z.Orientation));
    }

    [Fact]
    public async Task Handle_ZoneWithVerticalOrientation_DtoPropagatesVertical()
    {
        var verticalZone = new DockZone(Guid.NewGuid())
        {
            Name = "VerticalZone",
            Orientation = ZoneOrientation.Vertical
        };
        var table = new DockTable(Guid.NewGuid()) { TableKey = "N" };
        table.AssignToZone(verticalZone);
        AddDockToTable(table, Guid.NewGuid(), "N-A01", DockSide.A);
        ((List<DockTable>)verticalZone.Tables).Add(table);

        _repository.GetAllZonesWithDocksAsync(Arg.Any<CancellationToken>())
            .Returns([verticalZone]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal(ZoneOrientation.Vertical, result.Zones[0].Orientation);
    }

    // ──────────────────────────────────────────────────────────
    // Locator tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TableWithLocator_DtoContainsLocator()
    {
        var zone = new DockZone(Guid.NewGuid()) { Name = "Nostromo" };
        var table = new DockTable(Guid.NewGuid()) { TableKey = "N", Locator = "Mesa Ventana" };
        table.AssignToZone(zone);
        AddDockToTable(table, Guid.NewGuid(), "N-A01", DockSide.A);
        AddDockToTable(table, Guid.NewGuid(), "N-B01", DockSide.B);
        ((List<DockTable>)zone.Tables).Add(table);

        _repository.GetAllZonesWithDocksAsync(Arg.Any<CancellationToken>())
            .Returns([zone]);

        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.Equal("Mesa Ventana", result.Zones[0].Tables[0].Locator);
    }

    [Fact]
    public async Task Handle_TableWithoutLocator_DtoLocatorIsNull()
    {
        var result = await _handler.Handle(Query(), CancellationToken.None);

        Assert.All(
            result.Zones.SelectMany(z => z.Tables),
            t => Assert.Null(t.Locator));
    }
}
