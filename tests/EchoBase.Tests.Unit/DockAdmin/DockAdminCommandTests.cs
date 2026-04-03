using EchoBase.Core.DockAdmin;
using EchoBase.Core.DockAdmin.Commands;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Core.SystemAdmin;
using MediatR;
using NSubstitute;

namespace EchoBase.Tests.Unit.DockAdmin;

public class DockAdminCommandTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ZoneId  = Guid.NewGuid();
    private static readonly Guid DockId  = Guid.NewGuid();
    private static readonly Guid TableId = Guid.NewGuid();

    private readonly IBlockedDockRepository _blockedDockRepo  = Substitute.For<IBlockedDockRepository>();
    private readonly IDockAdminRepository   _dockAdminRepo    = Substitute.For<IDockAdminRepository>();
    private readonly IPublisher             _publisher        = Substitute.For<IPublisher>();
    private readonly TimeProvider           _time             = Substitute.For<TimeProvider>();

    // ── Handlers ─────────────────────────────────────────────────────────────

    private CreateDockZoneHandler  ZoneCreateHandler()    => new(_blockedDockRepo, _dockAdminRepo);
    private UpdateDockZoneHandler  ZoneUpdateHandler()    => new(_blockedDockRepo, _dockAdminRepo);
    private DeleteDockZoneHandler  ZoneDeleteHandler()    => new(_blockedDockRepo, _dockAdminRepo);
    private ReorderDockZonesHandler ZoneReorderHandler()  => new(_blockedDockRepo, _dockAdminRepo);
    private CreateDockHandler      DockCreateHandler()    => new(_blockedDockRepo, _dockAdminRepo);
    private UpdateDockHandler      DockUpdateHandler()    => new(_blockedDockRepo, _dockAdminRepo);
    private DeleteDockHandler      DockDeleteHandler()    => new(_blockedDockRepo, _dockAdminRepo, _time, _publisher);
    private CreateDockTableHandler TableCreateHandler()   => new(_blockedDockRepo, _dockAdminRepo);
    private UpdateDockTableHandler TableUpdateHandler()   => new(_blockedDockRepo, _dockAdminRepo);
    private DeleteDockTableHandler TableDeleteHandler()   => new(_blockedDockRepo, _dockAdminRepo);
    private ReorderDockTablesHandler TableReorderHandler() => new(_blockedDockRepo, _dockAdminRepo);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AsSystemAdmin()     => _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);
    private void AsNonSystemAdmin()  => _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

    private static DockZone MakeZone(Guid? id = null, string name = "TestZone", int dockCount = 0)
    {
        var zone = new DockZone(id ?? ZoneId) { Name = name, Orientation = ZoneOrientation.Horizontal };
        if (dockCount > 0)
        {
            var table = new DockTable(Guid.NewGuid()) { TableKey = "T" };
            table.AssignToZone(zone);
            for (var i = 0; i < dockCount; i++)
            {
                var dock = new Dock(Guid.NewGuid()) { Code = $"X-{i:D2}", Location = "L", Equipment = "E" };
                dock.AssignToTable(table, DockSide.A);
                ((List<Dock>)table.Docks).Add(dock);
            }
            ((List<DockTable>)zone.Tables).Add(table);
        }
        return zone;
    }

    private static Dock MakeDock(Guid? id = null, string code = "A-01") =>
        new(id ?? DockId) { Code = code, Location = "Sala A", Equipment = "Monitor" };

    private static DockTable MakeTable(Guid? id = null, string key = "N") =>
        new(id ?? TableId) { TableKey = key };

    private static DateTimeOffset FrozenNow => new(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-01..03 — CreateDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDockZone_ValidRequest_ReturnsSuccessWithGuid()
    {
        AsSystemAdmin();
        _dockAdminRepo.ZoneNameExistsAsync("Nave", null, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateDockZoneCommand(AdminId, "Nave", null, ZoneOrientation.Horizontal);
        var result = await ZoneCreateHandler().Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _dockAdminRepo.Received(1).AddZoneAsync(Arg.Is<DockZone>(z => z.Name == "Nave"), Arg.Any<CancellationToken>());
        await _dockAdminRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDockZone_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await ZoneCreateHandler().Handle(
            new CreateDockZoneCommand(AdminId, "X", null, ZoneOrientation.Horizontal), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
        await _dockAdminRepo.DidNotReceive().AddZoneAsync(Arg.Any<DockZone>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDockZone_DuplicateName_ReturnsZoneNameAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.ZoneNameExistsAsync("Nave", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await ZoneCreateHandler().Handle(
            new CreateDockZoneCommand(AdminId, "Nave", null, ZoneOrientation.Horizontal), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneNameAlreadyExists, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-04..07 — UpdateDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDockZone_ValidRequest_ReturnsSuccess()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone());
        _dockAdminRepo.ZoneNameExistsAsync("NuevoNombre", ZoneId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await ZoneUpdateHandler().Handle(
            new UpdateDockZoneCommand(AdminId, ZoneId, "NuevoNombre", "Desc", ZoneOrientation.Vertical),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).UpdateZoneAsync(ZoneId, "NuevoNombre", "Desc", ZoneOrientation.Vertical, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDockZone_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await ZoneUpdateHandler().Handle(
            new UpdateDockZoneCommand(AdminId, ZoneId, "X", null, ZoneOrientation.Horizontal), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task UpdateDockZone_ZoneNotFound_ReturnsZoneNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns((DockZone?)null);

        var result = await ZoneUpdateHandler().Handle(
            new UpdateDockZoneCommand(AdminId, ZoneId, "X", null, ZoneOrientation.Horizontal), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateDockZone_DuplicateName_ReturnsZoneNameAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone());
        _dockAdminRepo.ZoneNameExistsAsync("Duplicado", ZoneId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await ZoneUpdateHandler().Handle(
            new UpdateDockZoneCommand(AdminId, ZoneId, "Duplicado", null, ZoneOrientation.Horizontal),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneNameAlreadyExists, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-08..11 — DeleteDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDockZone_EmptyZone_ReturnsSuccess()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone());

        var result = await ZoneDeleteHandler().Handle(
            new DeleteDockZoneCommand(AdminId, ZoneId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).DeleteZoneAsync(Arg.Any<DockZone>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDockZone_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await ZoneDeleteHandler().Handle(
            new DeleteDockZoneCommand(AdminId, ZoneId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task DeleteDockZone_ZoneNotFound_ReturnsZoneNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns((DockZone?)null);

        var result = await ZoneDeleteHandler().Handle(
            new DeleteDockZoneCommand(AdminId, ZoneId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneNotFound, result.Error);
    }

    [Fact]
    public async Task DeleteDockZone_ZoneHasDocks_ReturnsZoneHasDocks()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone(dockCount: 2));

        var result = await ZoneDeleteHandler().Handle(
            new DeleteDockZoneCommand(AdminId, ZoneId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneHasDocks, result.Error);
        await _dockAdminRepo.DidNotReceive().DeleteZoneAsync(Arg.Any<DockZone>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-12..16 — CreateDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDock_ValidRequest_ReturnsSuccessWithGuid()
    {
        AsSystemAdmin();
        _dockAdminRepo.DockCodeExistsAsync("A-01", null, Arg.Any<CancellationToken>()).Returns(false);
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());

        var result = await DockCreateHandler().Handle(
            new CreateDockCommand(AdminId, TableId, DockSide.A, "A-01", "Sala A", "Monitor"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _dockAdminRepo.Received(1).AddDockAsync(Arg.Is<Dock>(d => d.Code == "A-01"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDock_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await DockCreateHandler().Handle(
            new CreateDockCommand(AdminId, TableId, DockSide.A, "A-01", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task CreateDock_EmptyCode_ReturnsDockCodeRequired()
    {
        AsSystemAdmin();

        var result = await DockCreateHandler().Handle(
            new CreateDockCommand(AdminId, TableId, DockSide.A, "   ", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockCodeRequired, result.Error);
    }

    [Fact]
    public async Task CreateDock_DuplicateCode_ReturnsDockCodeAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.DockCodeExistsAsync("A-01", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await DockCreateHandler().Handle(
            new CreateDockCommand(AdminId, TableId, DockSide.A, "A-01", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockCodeAlreadyExists, result.Error);
    }

    [Fact]
    public async Task CreateDock_TableNotFound_ReturnsTableNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.DockCodeExistsAsync("A-01", null, Arg.Any<CancellationToken>()).Returns(false);
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns((DockTable?)null);

        var result = await DockCreateHandler().Handle(
            new CreateDockCommand(AdminId, TableId, DockSide.A, "A-01", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableNotFound, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-17..21 — UpdateDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDock_ValidRequest_ReturnsSuccess()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns(MakeDock());
        _dockAdminRepo.DockCodeExistsAsync("B-02", DockId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await DockUpdateHandler().Handle(
            new UpdateDockCommand(AdminId, DockId, "B-02", "Sala B", "Portátil"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).UpdateDockAsync(DockId, "B-02", "Sala B", "Portátil", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDock_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await DockUpdateHandler().Handle(
            new UpdateDockCommand(AdminId, DockId, "A-01", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task UpdateDock_EmptyCode_ReturnsDockCodeRequired()
    {
        AsSystemAdmin();

        var result = await DockUpdateHandler().Handle(
            new UpdateDockCommand(AdminId, DockId, "", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockCodeRequired, result.Error);
    }

    [Fact]
    public async Task UpdateDock_DockNotFound_ReturnsDockNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns((Dock?)null);

        var result = await DockUpdateHandler().Handle(
            new UpdateDockCommand(AdminId, DockId, "A-01", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateDock_DuplicateCode_ReturnsDockCodeAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns(MakeDock());
        _dockAdminRepo.DockCodeExistsAsync("Z-99", DockId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await DockUpdateHandler().Handle(
            new UpdateDockCommand(AdminId, DockId, "Z-99", "L", "E"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockCodeAlreadyExists, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-22..25 — DeleteDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDock_WithFutureReservations_CancelsThenDeletesAndReturnsCount()
    {
        AsSystemAdmin();
        _time.GetUtcNow().Returns(FrozenNow);
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns(MakeDock());

        var today = DateOnly.FromDateTime(FrozenNow.UtcDateTime);
        var reservations = new List<Reservation>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), DockId, today.AddDays(1), TimeSlot.Morning)
        };
        _dockAdminRepo.GetFutureActiveReservationsForDockAsync(DockId, today, Arg.Any<CancellationToken>())
            .Returns(reservations);

        var result = await DockDeleteHandler().Handle(
            new DeleteDockCommand(AdminId, DockId, "Obsoleto"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        await _publisher.Received(1).Publish(Arg.Any<ReservationCancelledNotification>(), Arg.Any<CancellationToken>());
        await _dockAdminRepo.Received(1).DeleteAllReservationsForDockAsync(DockId, Arg.Any<CancellationToken>());
        await _dockAdminRepo.Received(1).DeleteDockAsync(Arg.Any<Dock>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDock_NoFutureReservations_ReturnsZeroCount()
    {
        AsSystemAdmin();
        _time.GetUtcNow().Returns(FrozenNow);
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns(MakeDock());
        _dockAdminRepo.GetFutureActiveReservationsForDockAsync(DockId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        var result = await DockDeleteHandler().Handle(
            new DeleteDockCommand(AdminId, DockId, "Obsoleto"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
        await _publisher.DidNotReceive().Publish(Arg.Any<ReservationCancelledNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDock_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await DockDeleteHandler().Handle(
            new DeleteDockCommand(AdminId, DockId, "Razón"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task DeleteDock_DockNotFound_ReturnsDockNotFound()
    {
        AsSystemAdmin();
        _time.GetUtcNow().Returns(FrozenNow);
        _dockAdminRepo.GetDockByIdAsync(DockId, Arg.Any<CancellationToken>()).Returns((Dock?)null);

        var result = await DockDeleteHandler().Handle(
            new DeleteDockCommand(AdminId, DockId, "Razón"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.DockNotFound, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-26..31 — CreateDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDockTable_ValidRequest_ReturnsSuccessWithGuid()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone());
        _dockAdminRepo.TableKeyExistsInZoneAsync("N", ZoneId, null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await TableCreateHandler().Handle(
            new CreateDockTableCommand(AdminId, ZoneId, "N", "Mesa Norte"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _dockAdminRepo.Received(1).AddTableAsync(Arg.Is<DockTable>(t => t.TableKey == "N"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDockTable_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await TableCreateHandler().Handle(
            new CreateDockTableCommand(AdminId, ZoneId, "N", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task CreateDockTable_EmptyKey_ReturnsTableKeyRequired()
    {
        AsSystemAdmin();

        var result = await TableCreateHandler().Handle(
            new CreateDockTableCommand(AdminId, ZoneId, "  ", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableKeyRequired, result.Error);
    }

    [Fact]
    public async Task CreateDockTable_ZoneNotFound_ReturnsZoneNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns((DockZone?)null);

        var result = await TableCreateHandler().Handle(
            new CreateDockTableCommand(AdminId, ZoneId, "N", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.ZoneNotFound, result.Error);
    }

    [Fact]
    public async Task CreateDockTable_DuplicateKeyInZone_ReturnsTableKeyAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetZoneByIdAsync(ZoneId, Arg.Any<CancellationToken>()).Returns(MakeZone());
        _dockAdminRepo.TableKeyExistsInZoneAsync("N", ZoneId, null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await TableCreateHandler().Handle(
            new CreateDockTableCommand(AdminId, ZoneId, "N", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableKeyAlreadyExists, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-32..34 — UpdateDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDockTable_ValidRequest_ReturnsSuccess()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());
        _dockAdminRepo.TableKeyExistsInZoneAsync("N", Arg.Any<Guid>(), TableId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await TableUpdateHandler().Handle(
            new UpdateDockTableCommand(AdminId, TableId, "N", "Nuevo localizador"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).UpdateTableAsync(TableId, "N", "Nuevo localizador", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDockTable_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await TableUpdateHandler().Handle(
            new UpdateDockTableCommand(AdminId, TableId, "N", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task UpdateDockTable_TableNotFound_ReturnsTableNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns((DockTable?)null);

        var result = await TableUpdateHandler().Handle(
            new UpdateDockTableCommand(AdminId, TableId, "N", "X"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateDockTable_EmptyKey_ReturnsTableKeyRequired()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());

        var result = await TableUpdateHandler().Handle(
            new UpdateDockTableCommand(AdminId, TableId, "  ", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableKeyRequired, result.Error);
    }

    [Fact]
    public async Task UpdateDockTable_DuplicateKey_ReturnsTableKeyAlreadyExists()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());
        _dockAdminRepo.TableKeyExistsInZoneAsync("OTRA", Arg.Any<Guid>(), TableId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await TableUpdateHandler().Handle(
            new UpdateDockTableCommand(AdminId, TableId, "OTRA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableKeyAlreadyExists, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-35..37 — DeleteDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDockTable_ValidRequest_ReturnsSuccess()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());

        var result = await TableDeleteHandler().Handle(
            new DeleteDockTableCommand(AdminId, TableId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).DeleteTableAsync(Arg.Any<DockTable>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDockTable_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await TableDeleteHandler().Handle(
            new DeleteDockTableCommand(AdminId, TableId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task DeleteDockTable_TableNotFound_ReturnsTableNotFound()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns((DockTable?)null);

        var result = await TableDeleteHandler().Handle(
            new DeleteDockTableCommand(AdminId, TableId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableNotFound, result.Error);
    }

    [Fact]
    public async Task DeleteDockTable_TableHasDocks_ReturnsTableHasDocks()
    {
        AsSystemAdmin();
        _dockAdminRepo.GetTableByIdAsync(TableId, Arg.Any<CancellationToken>()).Returns(MakeTable());
        _dockAdminRepo.TableHasDocksAsync(TableId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await TableDeleteHandler().Handle(
            new DeleteDockTableCommand(AdminId, TableId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DockAdminErrors.TableHasDocks, result.Error);
        await _dockAdminRepo.DidNotReceive().DeleteTableAsync(Arg.Any<DockTable>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-38..40 — ReorderDockZones
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ReorderDockZones_ValidRequest_CallsUpdateZoneOrdersWithCorrectItems()
    {
        AsSystemAdmin();
        var orderedIds = new List<Guid> { ZoneId, Guid.NewGuid() };

        var result = await ZoneReorderHandler().Handle(
            new ReorderDockZonesCommand(AdminId, orderedIds), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).UpdateZoneOrdersAsync(
            Arg.Is<IReadOnlyList<(Guid Id, int Order)>>(items =>
                items.Count == 2 &&
                items[0].Id == orderedIds[0] && items[0].Order == 0 &&
                items[1].Id == orderedIds[1] && items[1].Order == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderDockZones_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await ZoneReorderHandler().Handle(
            new ReorderDockZonesCommand(AdminId, [ZoneId]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
        await _dockAdminRepo.DidNotReceive().UpdateZoneOrdersAsync(Arg.Any<IReadOnlyList<(Guid, int)>>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UT-DA-41..43 — ReorderDockTables
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ReorderDockTables_ValidRequest_CallsUpdateTableOrdersWithCorrectItems()
    {
        AsSystemAdmin();
        var orderedIds = new List<Guid> { TableId, Guid.NewGuid(), Guid.NewGuid() };

        var result = await TableReorderHandler().Handle(
            new ReorderDockTablesCommand(AdminId, ZoneId, orderedIds), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _dockAdminRepo.Received(1).UpdateTableOrdersAsync(
            Arg.Is<IReadOnlyList<(Guid Id, int Order)>>(items =>
                items.Count == 3 &&
                items[0].Id == orderedIds[0] && items[0].Order == 0 &&
                items[2].Id == orderedIds[2] && items[2].Order == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderDockTables_NotSystemAdmin_ReturnsNotSystemAdmin()
    {
        AsNonSystemAdmin();

        var result = await TableReorderHandler().Handle(
            new ReorderDockTablesCommand(AdminId, ZoneId, [TableId]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
        await _dockAdminRepo.DidNotReceive().UpdateTableOrdersAsync(Arg.Any<IReadOnlyList<(Guid, int)>>(), Arg.Any<CancellationToken>());
    }
}
