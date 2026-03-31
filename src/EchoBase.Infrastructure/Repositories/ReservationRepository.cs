using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IReservationRepository"/> usando EF Core.
/// </summary>
internal sealed class ReservationRepository(EchoBaseDbContext context) : IReservationRepository
{
    /// <inheritdoc />
    public Task<bool> DockExistsAsync(Guid dockId, CancellationToken ct = default) =>
        context.Docks.AnyAsync(d => d.Id == dockId, ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetActiveDockReservationsAsync(
        Guid dockId, DateOnly date, CancellationToken ct = default) =>
        context.Reservations
            .Where(r => r.DockId == dockId && r.Date == date && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetActiveUserReservationsAsync(
        Guid userId, DateOnly date, CancellationToken ct = default) =>
        context.Reservations
            .Where(r => r.UserId == userId && r.Date == date && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc />
    public async Task AddAsync(Reservation reservation, CancellationToken ct = default) =>
        await context.Reservations.AddAsync(reservation, ct);

    /// <inheritdoc />
    public async Task<string?> GetDockCodeAsync(Guid dockId, CancellationToken ct = default) =>
        await context.Docks
            .Where(d => d.Id == dockId)
            .Select(d => d.Code)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetUserReservationsAsync(Guid userId, CancellationToken ct = default) =>
        context.Reservations
            .Include(r => r.Dock)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Status)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetActiveReservationsForDateAsync(DateOnly date, CancellationToken ct = default) =>
        context.Reservations
            .Include(r => r.Dock)
            .Include(r => r.User)
            .Where(r => r.Date == date && r.Status == ReservationStatus.Active)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetActiveReservationsForDocksInRangeAsync(
        IReadOnlyList<Guid> dockIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default) =>
        context.Reservations
            .Include(r => r.Dock)
            .Where(r => dockIds.Contains(r.DockId)
                        && r.Status == ReservationStatus.Active
                        && r.Date >= startDate
                        && r.Date <= endDate)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);

    /// <inheritdoc />
    public Task<List<Reservation>> GetActiveReservationsInRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Guid>? dockIds,
        CancellationToken ct = default)
    {
        var query = context.Reservations
            .Include(r => r.Dock)
            .Include(r => r.User)
            .Where(r => r.Status == ReservationStatus.Active
                        && r.Date >= startDate
                        && r.Date <= endDate);

        if (dockIds is { Count: > 0 })
            query = query.Where(r => dockIds.Contains(r.DockId));

        return query.ToListAsync(ct);
    }
}
