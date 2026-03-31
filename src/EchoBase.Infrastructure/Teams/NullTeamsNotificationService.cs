using EchoBase.Core.Interfaces;

namespace EchoBase.Infrastructure.Teams;

/// <summary>
/// Implementación no operativa de <see cref="ITeamsNotificationService"/>.
/// Se registra cuando la funcionalidad de Teams está desactivada mediante el
/// feature flag <c>Features:TeamsNotificationsEnabled</c>. Todas las llamadas
/// se completan inmediatamente sin realizar ninguna operación externa.
/// </summary>
internal sealed class NullTeamsNotificationService : ITeamsNotificationService
{
    /// <inheritdoc/>
    public Task SendChatMessageAsync(string userId, string message, CancellationToken ct = default)
        => Task.CompletedTask;
}
