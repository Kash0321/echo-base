using EchoBase.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EchoBase.Infrastructure.Teams;

/// <summary>
/// Implementación de desarrollo que registra los mensajes de Teams en el log
/// en lugar de enviarlos por Microsoft Graph.
/// </summary>
internal sealed class LogTeamsNotificationService(
    ILogger<LogTeamsNotificationService> logger) : ITeamsNotificationService
{
    public Task SendChatMessageAsync(string userId, string message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV-TEAMS] Usuario: {UserId} | Mensaje: {Message}",
            userId, message);

        return Task.CompletedTask;
    }
}
