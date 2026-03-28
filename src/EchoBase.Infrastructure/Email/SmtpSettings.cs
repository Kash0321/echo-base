namespace EchoBase.Infrastructure.Email;

/// <summary>
/// Configuración SMTP para el envío de correos electrónicos vía MailKit.
/// Se mapea desde la sección <c>Smtp</c> de appsettings.json.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    /// <summary>Servidor SMTP (ej.: smtp.office365.com).</summary>
    public required string Host { get; init; }

    /// <summary>Puerto SMTP (por defecto 587 para STARTTLS).</summary>
    public int Port { get; init; } = 587;

    /// <summary>Usar SSL/TLS.</summary>
    public bool UseSsl { get; init; } = true;

    /// <summary>Nombre de usuario para autenticación SMTP.</summary>
    public required string UserName { get; init; }

    /// <summary>Contraseña para autenticación SMTP.</summary>
    public required string Password { get; init; }

    /// <summary>Dirección del remitente.</summary>
    public required string FromAddress { get; init; }

    /// <summary>Nombre del remitente.</summary>
    public string FromName { get; init; } = "EchoBase";
}
