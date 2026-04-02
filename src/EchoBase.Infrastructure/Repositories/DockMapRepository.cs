using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IDockMapRepository"/> usando EF Core.
/// </summary>
internal sealed class DockMapRepository(EchoBaseDbContext context) : IDockMapRepository
{
    /// <inheritdoc />
    public Task<List<DockZone>> GetAllZonesWithDocksAsync(CancellationToken ct = default) =>
        context.DockZones
            .Include(z => z.Docks)
            .Include(z => z.Tables)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetAllActiveReservationsForDateAsync(
        DateOnly date, CancellationToken ct = default) =>
        context.Reservations
            .Include(r => r.User)
            .Where(r => r.Date == date && r.Status == ReservationStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<BlockedDock>> GetBlockedDocksForDateAsync(
        DateOnly date, CancellationToken ct = default) =>
        context.BlockedDocks
            .Include(b => b.BlockedByUser)
            .Where(b => b.IsActive && b.StartDate <= date && b.EndDate >= date)
            .AsNoTracking()
            .ToListAsync(ct);
}
