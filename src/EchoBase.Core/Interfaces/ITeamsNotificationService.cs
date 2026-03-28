namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción para el envío de notificaciones a Microsoft Teams.
/// </summary>
public interface ITeamsNotificationService
{
    /// <summary>
    /// Envía un mensaje de chat a un usuario de Teams.
    /// </summary>
    /// <param name="userId">Identificador Azure AD del usuario destinatario.</param>
    /// <param name="message">Contenido del mensaje en texto plano o HTML.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task SendChatMessageAsync(string userId, string message, CancellationToken ct = default);
}
