using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Repositories;

/// <summary>
/// Implementación de <see cref="IUserRepository"/> usando EF Core.
/// </summary>
internal sealed class UserRepository(EchoBaseDbContext context) : IUserRepository
{
    /// <inheritdoc />
    public async Task<UserContactInfo?> GetContactInfoAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserContactInfo(u.Email, u.Name))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task EnsureUserAsync(Guid userId, string name, string email, CancellationToken ct = default)
    {
        bool exists = await context.Users.AnyAsync(u => u.Id == userId, ct);
        if (!exists)
        {
            var user = new User(userId) { Name = name, Email = email };
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto(
                u.Id,
                u.Name,
                u.Email,
                u.BusinessLine,
                u.PhoneNumber,
                u.EmailNotifications,
                u.TeamsNotifications))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetForUpdateAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserWithRolesDto>> GetAllWithRolesAsync(CancellationToken ct = default)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Roles)
            .OrderBy(u => u.Name)
            .Select(u => new UserWithRolesDto(
                u.Id,
                u.Name,
                u.Email,
                u.Roles.Select(r => r.Name).ToList()))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName, ct);
    }
}
