using EchoBase.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EchoBase.Infrastructure.Email;

/// <summary>
/// Implementación de desarrollo que registra los emails en el log
/// en lugar de enviarlos por SMTP.
/// </summary>
internal sealed class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[DEV-EMAIL] Para: {To} | Asunto: {Subject} | Cuerpo: {Body}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
