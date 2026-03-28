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
}
