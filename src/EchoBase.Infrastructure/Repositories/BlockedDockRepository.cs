using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IBlockedDockRepository"/> usando EF Core.
/// </summary>
internal sealed class BlockedDockRepository(EchoBaseDbContext context) : IBlockedDockRepository
{
    /// <inheritdoc />
    public Task<bool> IsDockBlockedAsync(Guid dockId, DateOnly date, CancellationToken ct = default) =>
        context.BlockedDocks.AnyAsync(
            b => b.DockId == dockId
                 && b.IsActive
                 && b.StartDate <= date
                 && b.EndDate >= date,
            ct);

    /// <inheritdoc />
    public Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken ct = default) =>
        context.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .AnyAsync(r => r.Name == roleName, ct);

    /// <inheritdoc />
    public async Task<bool> AllDocksExistAsync(IReadOnlyList<Guid> dockIds, CancellationToken ct = default)
    {
        var count = await context.Docks.CountAsync(d => dockIds.Contains(d.Id), ct);
        return count == dockIds.Count;
    }

    /// <inheritdoc />
    public Task<List<BlockedDock>> GetActiveBlocksForDocksAsync(
        IReadOnlyList<Guid> dockIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default) =>
        context.BlockedDocks
            .Where(b => dockIds.Contains(b.DockId)
                        && b.IsActive
                        && b.StartDate <= endDate
                        && b.EndDate >= startDate)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<BlockedDock>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default) =>
        context.BlockedDocks
            .Where(b => ids.Contains(b.Id))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<BlockedDock> blocks, CancellationToken ct = default) =>
        await context.BlockedDocks.AddRangeAsync(blocks, ct);

    /// <inheritdoc />
    public Task<List<BlockedDock>> GetAllActiveBlocksAsync(CancellationToken ct = default) =>
        context.BlockedDocks
            .Include(b => b.Dock)
            .Include(b => b.BlockedByUser)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.StartDate)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
