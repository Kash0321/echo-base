using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IAuditLogRepository"/> usando EF Core.
/// </summary>
internal sealed class AuditLogRepository(EchoBaseDbContext context) : IAuditLogRepository
{
    /// <inheritdoc />
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
        => await context.AuditLogs.AddAsync(entry, ct);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<AuditLogDto> Items, int TotalCount)> GetPagedAsync(
        DateOnly? fromDate,
        DateOnly? toDate,
        AuditAction? actionFilter,
        string? userSearch,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        // 1. Load all logs (with action filter only — date/sort done in .NET for SQLite compat)
        var logQuery = context.AuditLogs.AsNoTracking().AsQueryable();

        if (actionFilter.HasValue)
            logQuery = logQuery.Where(l => l.Action == actionFilter.Value);

        var rawLogs = await logQuery.ToListAsync(ct);

        // 2. Apply date filters and sort in .NET (DateTimeOffset properties not translateable in SQLite)
        IEnumerable<AuditLog> logsEnumerable = rawLogs;

        if (fromDate.HasValue)
        {
            var fromDt = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            logsEnumerable = logsEnumerable.Where(l => l.Timestamp >= fromDt);
        }

        if (toDate.HasValue)
        {
            var toDt = new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            logsEnumerable = logsEnumerable.Where(l => l.Timestamp <= toDt);
        }

        var allLogs = logsEnumerable.OrderByDescending(l => l.Timestamp).ToList();

        // 3. Resolve user names in .NET (avoids nullable Guid join issue)
        var userIds = allLogs
            .Where(l => l.PerformedByUserId.HasValue)
            .Select(l => l.PerformedByUserId!.Value)
            .Distinct()
            .ToHashSet();

        Dictionary<Guid, string> userNames = [];
        if (userIds.Count > 0)
        {
            userNames = await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        }

        // 4. Apply user search filter in memory (after resolving names)
        IEnumerable<AuditLog> filtered = allLogs;
        if (!string.IsNullOrWhiteSpace(userSearch))
        {
            filtered = allLogs.Where(l =>
                l.PerformedByUserId.HasValue &&
                userNames.TryGetValue(l.PerformedByUserId.Value, out var name) &&
                name.Contains(userSearch, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        // 5. Paginate in memory
        var pageItems = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.PerformedByUserId,
                l.PerformedByUserId.HasValue && userNames.TryGetValue(l.PerformedByUserId.Value, out var n) ? n : null,
                l.Action,
                l.Details,
                l.Timestamp))
            .ToList();

        return (pageItems, totalCount);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
