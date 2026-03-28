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
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetAllActiveReservationsForDateAsync(
        DateOnly date, CancellationToken ct = default) =>
        context.Reservations
            .Where(r => r.Date == date && r.Status == ReservationStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Guid>> GetBlockedDockIdsForDateAsync(
        DateOnly date, CancellationToken ct = default) =>
        context.BlockedDocks
            .Where(b => b.IsActive && b.StartDate <= date && b.EndDate >= date)
            .Select(b => b.DockId)
            .Distinct()
            .ToListAsync(ct);
}
