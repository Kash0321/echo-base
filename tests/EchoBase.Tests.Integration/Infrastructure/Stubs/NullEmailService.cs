using EchoBase.Core.Interfaces;

namespace EchoBase.Tests.Integration.Infrastructure.Stubs;

/// <summary>
/// Implementación no-operativa de <see cref="IEmailService"/> para tests de integración.
/// Las llamadas a SendAsync se descartan silenciosamente para aislar los tests del servicio SMTP.
/// </summary>
internal sealed class NullEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        => Task.CompletedTask;
}
