using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Entrada de auditoría que registra una acción crítica realizada en el sistema.
/// </summary>
/// <remarks>
/// Se crea automáticamente a través de <c>AuditLoggingBehavior</c> para todos los comandos
/// que implementan <c>IAuditableRequest</c> y cuya ejecución resulta exitosa.
/// </remarks>
public sealed class AuditLog(Guid id) : EntityBase
{
    /// <summary>Identificador único de la entrada de auditoría.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>
    /// Identificador del usuario que realizó la acción.
    /// Puede ser <see langword="null"/> para acciones del sistema.
    /// </summary>
    public Guid? PerformedByUserId { get; init; }

    /// <summary>Acción que se registra en el log.</summary>
    public required AuditAction Action { get; init; }

    /// <summary>Descripción legible de los detalles de la acción (qué puestos, qué fechas, etc.).</summary>
    public required string Details { get; init; }

    /// <summary>Momento UTC en que se realizó la acción.</summary>
    public required DateTimeOffset Timestamp { get; init; }
}
