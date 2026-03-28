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
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
