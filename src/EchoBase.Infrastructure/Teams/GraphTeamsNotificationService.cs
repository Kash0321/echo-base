using Azure.Identity;
using EchoBase.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace EchoBase.Infrastructure.Teams;

/// <summary>
/// Implementación de <see cref="ITeamsNotificationService"/> usando Microsoft Graph SDK.
/// Envía mensajes de chat 1:1 al usuario mediante la API de Graph.
/// </summary>
internal sealed class GraphTeamsNotificationService : ITeamsNotificationService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphTeamsNotificationService> _logger;

    public GraphTeamsNotificationService(
        IOptions<GraphSettings> options,
        ILogger<GraphTeamsNotificationService> logger)
    {
        _logger = logger;
        var settings = options.Value;

        var credential = new ClientSecretCredential(
            settings.TenantId,
            settings.ClientId,
            settings.ClientSecret);

        _graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }

    /// <inheritdoc />
    public async Task SendChatMessageAsync(string userId, string message, CancellationToken ct = default)
    {
        try
        {
            // Crear un chat 1:1 entre la aplicación y el usuario
            var chat = new Chat
            {
                ChatType = ChatType.OneOnOne,
                Members = new List<ConversationMember>
                {
                    new AadUserConversationMember
                    {
                        Roles = ["owner"],
                        AdditionalData = new Dictionary<string, object>
                        {
                            { "user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{userId}')" }
                        }
                    }
                }
            };

            var createdChat = await _graphClient.Chats.PostAsync(chat, cancellationToken: ct);

            if (createdChat?.Id is null)
            {
                _logger.LogWarning("No se pudo crear el chat con el usuario {UserId}", userId);
                return;
            }

            // Enviar el mensaje al chat creado
            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = message
                }
            };

            await _graphClient.Chats[createdChat.Id].Messages
                .PostAsync(chatMessage, cancellationToken: ct);

            _logger.LogInformation("Mensaje de Teams enviado al usuario {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar mensaje de Teams al usuario {UserId}", userId);
        }
    }
}
