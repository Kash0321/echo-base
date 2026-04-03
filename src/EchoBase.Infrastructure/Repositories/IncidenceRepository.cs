using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación EF Core del repositorio de reportes de incidencias.
/// </summary>
internal sealed class IncidenceRepository(EchoBaseDbContext context) : IIncidenceRepository
{
    /// <inheritdoc/>
    public Task<IncidenceReport?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.IncidenceReports
            .Include(r => r.Dock)
            .Include(r => r.ReportedByUser)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    /// <inheritdoc/>
    public async Task AddAsync(IncidenceReport report, CancellationToken ct = default) =>
        await context.IncidenceReports.AddAsync(report, ct);

    /// <inheritdoc/>
    public Task<bool> DockExistsAsync(Guid dockId, CancellationToken ct = default) =>
        context.Docks.AnyAsync(d => d.Id == dockId, ct);

    /// <inheritdoc/>
    public Task<string?> GetDockCodeAsync(Guid dockId, CancellationToken ct = default) =>
        context.Docks
            .Where(d => d.Id == dockId)
            .Select(d => (string?)d.Code)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public Task<List<IncidenceReport>> GetUserIncidencesAsync(Guid userId, CancellationToken ct = default) =>
        context.IncidenceReports
            .Include(r => r.Dock)
            .Where(r => r.ReportedByUserId == userId)
            .OrderByDescending(r => r.Id)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<(List<IncidenceReport> Items, int TotalCount)> GetAllIncidencesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.IncidenceReports
            .Include(r => r.Dock)
            .Include(r => r.ReportedByUser)
            .OrderByDescending(r => r.Id)
            .AsNoTracking();

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc/>
    public Task SaveChangesAsync(CancellationToken ct = default) =>
        context.SaveChangesAsync(ct);
}
