using EchoBase.Core.DockAdmin.Commands;
using EchoBase.Core.DockAdmin.Queries;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.SystemAdmin;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.DockAdmin;

/// <summary>
/// Tests de integración para la Funcionalidad 5: Configuración de Zonas, Mesas y Puestos.
/// </summary>
public sealed class DockAdminIntegrationTests : IntegrationTestBase
{
    // ── GUIDs de datos maestros (DbSeeder) ────────────────────────────────────
    private static readonly Guid NostromoZoneId = new("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid DerelictZoneId = new("a0000000-0000-0000-0000-000000000002");
    // Uno de los puestos de Nostromo (N-A01) — véase DbSeeder
    private static readonly Guid DockNA01 = new("b0000000-0000-0000-0001-000000000001");

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-01..03 — CreateDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDockZone_ValidRequest_PersistedToDatabase()
    {
        var cmd = new CreateDockZoneCommand(AdminUserId, "Omega", "Sala nueva", ZoneOrientation.Vertical);

        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        var zone = await DbContext.DockZones.SingleOrDefaultAsync(z => z.Id == result.Value);
        Assert.NotNull(zone);
        Assert.Equal("Omega", zone.Name);
        Assert.Equal(ZoneOrientation.Vertical, zone.Orientation);
    }

    [Fact]
    public async Task CreateDockZone_AuditEntryWritten()
    {
        var cmd = new CreateDockZoneCommand(AdminUserId, "Audit-Zone", null, ZoneOrientation.Horizontal);
        await Mediator.Send(cmd);

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockZoneCreated && a.PerformedByUserId == AdminUserId);
        Assert.NotNull(entry);
        Assert.Contains("Audit-Zone", entry.Details);
    }

    [Fact]
    public async Task CreateDockZone_NotSystemAdmin_ReturnsError()
    {
        var cmd = new CreateDockZoneCommand(TestUserId, "X", null, ZoneOrientation.Horizontal);
        var result = await Mediator.Send(cmd);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
        Assert.False(await DbContext.DockZones.AnyAsync(z => z.Name == "X"));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-04..06 — UpdateDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDockZone_ValidRequest_ChangesPersistedToDatabase()
    {
        var cmd = new UpdateDockZoneCommand(AdminUserId, NostromoZoneId, "Nostromo-V2", "Desc actualizada", ZoneOrientation.Vertical);
        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        // ExecuteUpdateAsync bypassa el change tracker; limpiar para leer el estado real
        DbContext.ChangeTracker.Clear();
        var zone = await DbContext.DockZones.SingleAsync(z => z.Id == NostromoZoneId);
        Assert.Equal("Nostromo-V2", zone.Name);
        Assert.Equal(ZoneOrientation.Vertical, zone.Orientation);
    }

    [Fact]
    public async Task UpdateDockZone_AuditEntryWritten()
    {
        var cmd = new UpdateDockZoneCommand(AdminUserId, NostromoZoneId, "Nostromo", null, ZoneOrientation.Horizontal);
        await Mediator.Send(cmd);

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockZoneUpdated && a.PerformedByUserId == AdminUserId);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task UpdateDockZone_NotSystemAdmin_ReturnsError()
    {
        var cmd = new UpdateDockZoneCommand(TestUserId, NostromoZoneId, "X", null, ZoneOrientation.Horizontal);
        var result = await Mediator.Send(cmd);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-07..09 — DeleteDockZone
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDockZone_EmptyZone_RemovedFromDatabase()
    {
        // Crear una zona vacía primero
        var createResult = await Mediator.Send(
            new CreateDockZoneCommand(AdminUserId, "Temporal", null, ZoneOrientation.Horizontal));
        var newZoneId = createResult.Value;

        // Act
        var result = await Mediator.Send(new DeleteDockZoneCommand(AdminUserId, newZoneId));

        Assert.True(result.IsSuccess);
        Assert.False(await DbContext.DockZones.AnyAsync(z => z.Id == newZoneId));
    }

    [Fact]
    public async Task DeleteDockZone_ZoneWithDocks_ReturnsZoneHasDocks()
    {
        // Nostromo tiene puestos asignados
        var result = await Mediator.Send(new DeleteDockZoneCommand(AdminUserId, NostromoZoneId));

        Assert.False(result.IsSuccess);
        Assert.Equal("ZONE_HAS_DOCKS", result.Error);
        Assert.True(await DbContext.DockZones.AnyAsync(z => z.Id == NostromoZoneId));
    }

    [Fact]
    public async Task DeleteDockZone_NotSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new DeleteDockZoneCommand(TestUserId, NostromoZoneId));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-10..13 — CreateDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDock_ValidRequest_PersistedToZone()
    {
        var cmd = new CreateDockCommand(AdminUserId, NostromoZoneId, "N-Z99", "Fondo sala", "Monitor extragrande");
        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        var dock = await DbContext.Docks.SingleOrDefaultAsync(d => d.Id == result.Value);
        Assert.NotNull(dock);
        Assert.Equal("N-Z99", dock.Code);
        Assert.Equal(NostromoZoneId, dock.DockZoneId);
    }

    [Fact]
    public async Task CreateDock_AuditEntryWritten()
    {
        var cmd = new CreateDockCommand(AdminUserId, DerelictZoneId, "D-Z99", "Sala D", "Portátil");
        await Mediator.Send(cmd);

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockCreated && a.PerformedByUserId == AdminUserId);
        Assert.NotNull(entry);
        Assert.Contains("D-Z99", entry.Details);
    }

    [Fact]
    public async Task CreateDock_DuplicateCode_ReturnsError()
    {
        // N-A01 ya existe en DbSeeder
        var result = await Mediator.Send(
            new CreateDockCommand(AdminUserId, NostromoZoneId, "N-A01", "L", "E"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DOCK_CODE_ALREADY_EXISTS", result.Error);
    }

    [Fact]
    public async Task CreateDock_NotSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(
            new CreateDockCommand(TestUserId, NostromoZoneId, "N-Z99", "L", "E"));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-14..16 — UpdateDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDock_ValidRequest_ChangesPersistedToDatabase()
    {
        var cmd = new UpdateDockCommand(AdminUserId, DockNA01, "N-NUEVO", "Ubicación nueva", "Equipo nuevo");
        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        DbContext.ChangeTracker.Clear();
        var dock = await DbContext.Docks.SingleAsync(d => d.Id == DockNA01);
        Assert.Equal("N-NUEVO", dock.Code);
        Assert.Equal("Ubicación nueva", dock.Location);
        Assert.Equal("Equipo nuevo", dock.Equipment);
    }

    [Fact]
    public async Task UpdateDock_AuditEntryWritten()
    {
        await Mediator.Send(new UpdateDockCommand(AdminUserId, DockNA01, "N-A01", "L", "E"));

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockUpdated && a.PerformedByUserId == AdminUserId);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task UpdateDock_NotSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new UpdateDockCommand(TestUserId, DockNA01, "N-A01", "L", "E"));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-17..21 — DeleteDock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDock_NoFutureReservations_RemovedFromDatabase()
    {
        // Crear un puesto nuevo (sin reservas) y eliminarlo
        var createResult = await Mediator.Send(
            new CreateDockCommand(AdminUserId, NostromoZoneId, "TEMP-01", "Temporal", "Ninguno"));
        var tempDockId = createResult.Value;

        var result = await Mediator.Send(new DeleteDockCommand(AdminUserId, tempDockId, "Prueba de eliminación"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value); // 0 reservas canceladas
        Assert.False(await DbContext.Docks.AnyAsync(d => d.Id == tempDockId));
    }

    [Fact]
    public async Task DeleteDock_WithFutureReservation_CancelsReservationAndRemovesDock()
    {
        // Crear un puesto + reservarlo + eliminarlo
        var createResult = await Mediator.Send(
            new CreateDockCommand(AdminUserId, NostromoZoneId, "TEMP-02", "Temporal", "Ninguno"));
        var tempDockId = createResult.Value;

        // Insertar reserva futura para TestUser
        await DbContext.Reservations.AddAsync(new global::EchoBase.Core.Entities.Reservation(
            Guid.NewGuid(), TestUserId, tempDockId, Today.AddDays(1), global::EchoBase.Core.Entities.Enums.TimeSlot.Morning));
        await DbContext.SaveChangesAsync();
        // Limpiar el tracker para que DeleteAllReservationsForDockAsync (ExecuteDeleteAsync)
        // no deje entidades obsoletas que provoquen conflictos de FK al eliminar el puesto
        DbContext.ChangeTracker.Clear();

        // Eliminar el puesto
        var result = await Mediator.Send(new DeleteDockCommand(AdminUserId, tempDockId, "Retirado"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value); // 1 reserva cancelada
        Assert.False(await DbContext.Docks.AnyAsync(d => d.Id == tempDockId));
        Assert.False(await DbContext.Reservations.AnyAsync(r => r.DockId == tempDockId));
    }

    [Fact]
    public async Task DeleteDock_AuditEntryWritten()
    {
        var createResult = await Mediator.Send(
            new CreateDockCommand(AdminUserId, DerelictZoneId, "TEMP-03", "T", "N"));
        await Mediator.Send(new DeleteDockCommand(AdminUserId, createResult.Value, "Test"));

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockDeleted && a.PerformedByUserId == AdminUserId);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task DeleteDock_DockNotFound_ReturnsError()
    {
        var result = await Mediator.Send(new DeleteDockCommand(AdminUserId, Guid.NewGuid(), "Razón"));

        Assert.False(result.IsSuccess);
        Assert.Equal("DOCK_NOT_FOUND", result.Error);
    }

    [Fact]
    public async Task DeleteDock_NotSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new DeleteDockCommand(TestUserId, DockNA01, "Razón"));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-22..25 — CreateDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateDockTable_ValidRequest_PersistedToDatabase()
    {
        var cmd = new CreateDockTableCommand(AdminUserId, NostromoZoneId, "N-EXTRA", "Mesa extra");
        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        var table = await DbContext.DockTables.SingleOrDefaultAsync(t => t.Id == result.Value);
        Assert.NotNull(table);
        Assert.Equal("N-EXTRA", table.TableKey);
        Assert.Equal("Mesa extra", table.Locator);
        Assert.Equal(NostromoZoneId, table.DockZoneId);
    }

    [Fact]
    public async Task CreateDockTable_AuditEntryWritten()
    {
        await Mediator.Send(new CreateDockTableCommand(AdminUserId, DerelictZoneId, "D-EXTRA", null));

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockTableCreated);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task CreateDockTable_NotSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new CreateDockTableCommand(TestUserId, NostromoZoneId, "N-X", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    [Fact]
    public async Task CreateDockTable_DuplicateKeyInZone_ReturnsError()
    {
        // "N" ya existe en Nostromo (DbSeeder)
        var result = await Mediator.Send(new CreateDockTableCommand(AdminUserId, NostromoZoneId, "N", null));

        Assert.False(result.IsSuccess);
        Assert.Equal("TABLE_KEY_ALREADY_EXISTS", result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-26..28 — UpdateDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateDockTable_ValidRequest_LocatorChangedInDatabase()
    {
        // La tabla de Nostromo tiene Id = e0000000-0000-0000-0000-000000000001
        var tableId = new Guid("e0000000-0000-0000-0000-000000000001");
        var cmd = new UpdateDockTableCommand(AdminUserId, tableId, "Locator actualizado");
        var result = await Mediator.Send(cmd);

        Assert.True(result.IsSuccess);
        DbContext.ChangeTracker.Clear();
        var table = await DbContext.DockTables.SingleAsync(t => t.Id == tableId);
        Assert.Equal("Locator actualizado", table.Locator);
    }

    [Fact]
    public async Task UpdateDockTable_AuditEntryWritten()
    {
        var tableId = new Guid("e0000000-0000-0000-0000-000000000001");
        await Mediator.Send(new UpdateDockTableCommand(AdminUserId, tableId, "X"));

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockTableUpdated);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task UpdateDockTable_NotSystemAdmin_ReturnsError()
    {
        var tableId = new Guid("e0000000-0000-0000-0000-000000000001");
        var result = await Mediator.Send(new UpdateDockTableCommand(TestUserId, tableId, "X"));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-29..31 — DeleteDockTable
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteDockTable_ValidRequest_RemovedFromDatabase()
    {
        // Crear una mesa nueva y eliminarla
        var createResult = await Mediator.Send(
            new CreateDockTableCommand(AdminUserId, NostromoZoneId, "TEMP-TABLE", "Temporal"));
        var tableId = createResult.Value;

        var result = await Mediator.Send(new DeleteDockTableCommand(AdminUserId, tableId));

        Assert.True(result.IsSuccess);
        Assert.False(await DbContext.DockTables.AnyAsync(t => t.Id == tableId));
    }

    [Fact]
    public async Task DeleteDockTable_AuditEntryWritten()
    {
        var createResult = await Mediator.Send(
            new CreateDockTableCommand(AdminUserId, DerelictZoneId, "TEMP-TABLE-2", null));
        await Mediator.Send(new DeleteDockTableCommand(AdminUserId, createResult.Value));

        var entry = await DbContext.AuditLogs
            .SingleOrDefaultAsync(a => a.Action == AuditAction.DockTableDeleted);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task DeleteDockTable_NotSystemAdmin_ReturnsError()
    {
        var tableId = new Guid("e0000000-0000-0000-0000-000000000001");
        var result = await Mediator.Send(new DeleteDockTableCommand(TestUserId, tableId));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // IT-DA-32..33 — GetDockAdminData
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDockAdminData_ReturnsAllSeededZonesWithDocksAndTables()
    {
        var result = await Mediator.Send(new GetDockAdminDataQuery());

        Assert.NotEmpty(result);
        var nostromo = result.SingleOrDefault(z => z.Id == NostromoZoneId);
        Assert.NotNull(nostromo);
        Assert.NotEmpty(nostromo.Docks);
        Assert.NotEmpty(nostromo.Tables);
    }

    [Fact]
    public async Task GetDockAdminData_NewZoneAndDockAppearsAfterCreate()
    {
        await Mediator.Send(new CreateDockZoneCommand(AdminUserId, "ZonaQuery", null, ZoneOrientation.Horizontal));

        var result = await Mediator.Send(new GetDockAdminDataQuery());

        Assert.Contains(result, z => z.Name == "ZonaQuery");
    }
}
