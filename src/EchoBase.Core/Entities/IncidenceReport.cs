using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Representa un reporte de incidencia sobre un puesto de trabajo.
/// </summary>
/// <remarks>
/// Los usuarios pueden reportar problemas de equipamiento, limpieza u otros
/// relacionados con sus puestos de trabajo. Los Managers pueden actualizar el
/// estado de la incidencia y añadir comentarios sobre las acciones tomadas.
/// </remarks>
public sealed class IncidenceReport(
    Guid id,
    Guid dockId,
    Guid reportedByUserId,
    DateTimeOffset createdAt) : EntityBase
{
    /// <summary>Identificador único del reporte (UUID v7).</summary>
    public Guid Id { get; } = EnsureValidId(id, nameof(id));

    /// <summary>Identificador del puesto de trabajo afectado.</summary>
    public Guid DockId { get; } = EnsureValidId(dockId, nameof(dockId));

    /// <summary>Identificador del usuario que reportó la incidencia.</summary>
    public Guid ReportedByUserId { get; } = EnsureValidId(reportedByUserId, nameof(reportedByUserId));

    /// <summary>Descripción de la incidencia reportada.</summary>
    public required string Description { get; init; }

    /// <summary>Estado actual del reporte.</summary>
    public IncidenceStatus Status { get; private set; } = IncidenceStatus.Open;

    /// <summary>Momento UTC en que se creó el reporte.</summary>
    public DateTimeOffset CreatedAt { get; } = createdAt;

    /// <summary>Momento UTC de la última actualización de estado. <see langword="null"/> si no ha sido actualizado.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Identificador del Manager que realizó la última actualización de estado. <see langword="null"/> si no ha sido actualizado.</summary>
    public Guid? UpdatedByUserId { get; private set; }

    /// <summary>Comentario añadido por el Manager al actualizar el estado. <see langword="null"/> si no hay comentario.</summary>
    public string? ManagerComment { get; private set; }

    /// <summary>Puesto de trabajo afectado. Se carga mediante navegación de EF Core.</summary>
    public Dock? Dock { get; private set; }

    /// <summary>Usuario que reportó la incidencia. Se carga mediante navegación de EF Core.</summary>
    public User? ReportedByUser { get; private set; }

    /// <summary>
    /// Actualiza el estado del reporte y registra los datos del Manager que realiza la acción.
    /// </summary>
    /// <param name="newStatus">Nuevo estado de la incidencia.</param>
    /// <param name="managerId">Identificador del Manager que realiza el cambio.</param>
    /// <param name="updatedAt">Timestamp UTC de la actualización.</param>
    /// <param name="comment">Comentario opcional del Manager sobre la acción tomada.</param>
    public void UpdateStatus(IncidenceStatus newStatus, Guid managerId, DateTimeOffset updatedAt, string? comment)
    {
        Status = newStatus;
        UpdatedByUserId = managerId;
        UpdatedAt = updatedAt;
        ManagerComment = comment;
    }
}
