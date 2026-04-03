using EchoBase.Core.Interfaces;

namespace EchoBase.Tests.Integration.Infrastructure.Stubs;

/// <summary>
/// Implementación no-operativa de <see cref="ITeamsNotificationService"/> para tests de integración.
/// Las llamadas se descartan silenciosamente para aislar los tests del servicio Microsoft Graph.
/// </summary>
internal sealed class NullTeamsNotificationService : ITeamsNotificationService
{
    public Task SendChatMessageAsync(string userId, string message, CancellationToken ct = default)
        => Task.CompletedTask;
}
