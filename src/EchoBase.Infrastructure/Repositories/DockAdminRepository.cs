using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IDockAdminRepository"/> usando EF Core.
/// Proporciona acceso CRUD a zonas, mesas y puestos para el SystemAdmin.
/// </summary>
internal sealed class DockAdminRepository(EchoBaseDbContext context) : IDockAdminRepository
{
    // ── Zonas ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<List<DockZone>> GetAllZonesWithDetailsAsync(CancellationToken ct = default) =>
        context.DockZones
            .Include(z => z.Tables)
                .ThenInclude(t => t.Docks)
            .AsNoTracking()
            .OrderBy(z => z.Order).ThenBy(z => z.Name)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<DockZone?> GetZoneByIdAsync(Guid id, CancellationToken ct = default) =>
        context.DockZones
            .Include(z => z.Tables)
                .ThenInclude(t => t.Docks)
            .FirstOrDefaultAsync(z => z.Id == id, ct);

    /// <inheritdoc />
    public Task<bool> ZoneNameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default) =>
        context.DockZones
            .AnyAsync(z => z.Name == name && (excludeId == null || z.Id != excludeId), ct);

    /// <inheritdoc />
    public async Task AddZoneAsync(DockZone zone, CancellationToken ct = default)
    {
        await context.DockZones.AddAsync(zone, ct);
    }

    /// <inheritdoc />
    public async Task UpdateZoneAsync(Guid id, string name, string? description, ZoneOrientation orientation, CancellationToken ct = default)
    {
        await context.DockZones
            .Where(z => z.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(z => z.Name,        name)
                .SetProperty(z => z.Description, description)
                .SetProperty(z => z.Orientation, orientation), ct);
    }

    /// <inheritdoc />
    public async Task UpdateZoneOrdersAsync(IReadOnlyList<(Guid Id, int Order)> items, CancellationToken ct = default)
    {
        foreach (var (id, order) in items)
        {
            await context.DockZones
                .Where(z => z.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(z => z.Order, order), ct);
        }
    }

    /// <inheritdoc />
    public async Task DeleteZoneAsync(DockZone zone, CancellationToken ct = default)
    {
        context.DockZones.Remove(zone);
        await context.SaveChangesAsync(ct);
    }

    // ── Puestos ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<Dock?> GetDockByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Docks.FirstOrDefaultAsync(d => d.Id == id, ct);

    /// <inheritdoc />
    public Task<bool> DockCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default) =>
        context.Docks
            .AnyAsync(d => d.Code == code && (excludeId == null || d.Id != excludeId), ct);

    /// <inheritdoc />
    public async Task AddDockAsync(Dock dock, CancellationToken ct = default)
    {
        await context.Docks.AddAsync(dock, ct);
    }

    /// <inheritdoc />
    public async Task UpdateDockAsync(Guid id, string code, string location, string equipment, CancellationToken ct = default)
    {
        await context.Docks
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Code,      code)
                .SetProperty(d => d.Location,  location)
                .SetProperty(d => d.Equipment, equipment), ct);
    }

    /// <inheritdoc />
    public Task<List<Reservation>> GetFutureActiveReservationsForDockAsync(
        Guid dockId, DateOnly fromDate, CancellationToken ct = default) =>
        context.Reservations
            .Include(r => r.User)
            .Where(r => r.DockId == dockId
                     && r.Status == ReservationStatus.Active
                     && r.Date >= fromDate)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task DeleteAllBlockedDocksForDockAsync(Guid dockId, CancellationToken ct = default)
    {
        await context.BlockedDocks
            .Where(b => b.DockId == dockId)
            .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteAllReservationsForDockAsync(Guid dockId, CancellationToken ct = default)
    {
        await context.Reservations
            .Where(r => r.DockId == dockId)
            .ExecuteDeleteAsync(ct);

        // Limpiar el change tracker para que las entidades Reservation cargadas
        // previamente (en GetFutureActiveReservationsForDockAsync) no queden marcadas
        // como Unchanged con un DockId que ya no existe en BD, lo que provocaría
        // un error de FK en el SaveChangesAsync que AuditLoggingBehavior ejecuta
        // después del handler.
        context.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public async Task DeleteDockAsync(Dock dock, CancellationToken ct = default)
    {
        // Usar ExecuteDeleteAsync en lugar de Remove para evitar conflictos de FK
        // con entidades de Reservation que puedan estar aún en el change tracker
        // tras la llamada previa a DeleteAllReservationsForDockAsync (ExecuteDeleteAsync).
        await context.Docks
            .Where(d => d.Id == dock.Id)
            .ExecuteDeleteAsync(ct);
    }

    // ── Mesas ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<DockTable?> GetTableByIdAsync(Guid id, CancellationToken ct = default) =>
        context.DockTables.FirstOrDefaultAsync(t => t.Id == id, ct);

    /// <inheritdoc />
    public Task<bool> TableKeyExistsInZoneAsync(
        string tableKey, Guid zoneId, Guid? excludeId = null, CancellationToken ct = default) =>
        context.DockTables
            .AnyAsync(t => t.TableKey == tableKey
                        && t.DockZoneId == zoneId
                        && (excludeId == null || t.Id != excludeId), ct);

    /// <inheritdoc />
    public async Task AddTableAsync(DockTable table, CancellationToken ct = default)
    {
        await context.DockTables.AddAsync(table, ct);
    }

    /// <inheritdoc />
    public async Task UpdateTableAsync(Guid id, string tableKey, string? locator, CancellationToken ct = default)
    {
        await context.DockTables
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.TableKey, tableKey)
                .SetProperty(t => t.Locator,  locator), ct);
    }

    /// <inheritdoc />
    public async Task UpdateTableOrdersAsync(IReadOnlyList<(Guid Id, int Order)> items, CancellationToken ct = default)
    {
        foreach (var (id, order) in items)
        {
            await context.DockTables
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Order, order), ct);
        }
    }

    /// <inheritdoc />
    public Task<bool> TableHasDocksAsync(Guid tableId, CancellationToken ct = default) =>
        context.Docks.AnyAsync(d => d.DockTableId == tableId, ct);

    /// <inheritdoc />
    public async Task DeleteTableAsync(DockTable table, CancellationToken ct = default)
    {
        context.DockTables.Remove(table);
        await context.SaveChangesAsync(ct);
    }

    // ── Unidad de trabajo ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
