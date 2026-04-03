namespace EchoBase.Infrastructure.Teams;

/// <summary>
/// Configuración para las notificaciones de Microsoft Teams vía Graph API.
/// Se mapea desde la sección <c>MicrosoftGraph</c> de appsettings.json.
/// </summary>
public sealed class GraphSettings
{
    public const string SectionName = "MicrosoftGraph";

    /// <summary>Tenant ID de Azure AD.</summary>
    public required string TenantId { get; init; }

    /// <summary>Client ID de la app registration con permisos de Graph.</summary>
    public required string ClientId { get; init; }

    /// <summary>Client Secret para autenticación de la aplicación.</summary>
    public required string ClientSecret { get; init; }
}
