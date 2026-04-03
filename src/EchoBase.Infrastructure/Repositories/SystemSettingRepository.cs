using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="ISystemSettingRepository"/> usando EF Core.
/// </summary>
internal sealed class SystemSettingRepository(EchoBaseDbContext context) : ISystemSettingRepository
{
    /// <inheritdoc />
    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var setting = await context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        return setting?.Value;
    }

    /// <inheritdoc />
    public Task<SystemSetting?> GetSettingAsync(string key, CancellationToken ct = default)
        => context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);

    /// <inheritdoc />
    public async Task SetAsync(
        string key,
        string value,
        Guid? updatedByUserId,
        DateTimeOffset updatedAt,
        CancellationToken ct = default)
    {
        var existing = await context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key, ct);

        if (existing is null)
        {
            context.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = updatedAt,
                UpdatedByUserId = updatedByUserId,
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = updatedAt;
            existing.UpdatedByUserId = updatedByUserId;
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);
}
