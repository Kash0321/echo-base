namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción para el envío de correos electrónicos.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico a la dirección indicada.
    /// </summary>
    /// <param name="to">Dirección de correo del destinatario.</param>
    /// <param name="subject">Asunto del mensaje.</param>
    /// <param name="htmlBody">Cuerpo del mensaje en HTML.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
