using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Common;

/// <summary>
/// Interfaz marcadora para comandos MediatR cuyas ejecuciones exitosas
/// deben quedar registradas en el log de auditoría del sistema.
/// </summary>
/// <remarks>
/// El <c>AuditLoggingBehavior</c> detecta automáticamente todos los comandos
/// que implementen esta interfaz y escribe una entrada en el AuditLog solo
/// cuando el resultado es exitoso.
/// </remarks>
public interface IAuditableRequest
{
    /// <summary>
    /// Identificador del usuario que ejecuta el comando.
    /// Puede ser <see langword="null"/> para acciones del sistema.
    /// </summary>
    Guid? PerformedByUserId { get; }

    /// <summary>Acción que se registrará en el log de auditoría.</summary>
    AuditAction AuditAction { get; }

    /// <summary>Construye el texto descriptivo de los detalles de la auditoría.</summary>
    string BuildAuditDetails();
}
