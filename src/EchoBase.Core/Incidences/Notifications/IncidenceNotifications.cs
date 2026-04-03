using EchoBase.Core.Entities.Enums;
using MediatR;

namespace EchoBase.Core.Incidences.Notifications;

/// <summary>
/// Notificación publicada tras crear un reporte de incidencia exitosamente.
/// Los Managers son notificados para que puedan tomar medidas correctivas.
/// </summary>
/// <param name="IncidenceId">Identificador del reporte creado.</param>
/// <param name="DockCode">Código del puesto afectado (ej.: N-A01).</param>
/// <param name="ReportedByUserId">Identificador del usuario que reportó la incidencia.</param>
/// <param name="Description">Descripción de la incidencia.</param>
public sealed record IncidenceReportedNotification(
    Guid IncidenceId,
    string DockCode,
    Guid ReportedByUserId,
    string Description) : INotification;

/// <summary>
/// Notificación publicada cuando un Manager actualiza el estado de un reporte de incidencia.
/// El usuario que reportó la incidencia es notificado del cambio de estado.
/// </summary>
/// <param name="IncidenceId">Identificador del reporte actualizado.</param>
/// <param name="DockCode">Código del puesto afectado.</param>
/// <param name="ReportedByUserId">Identificador del usuario que reportó la incidencia.</param>
/// <param name="NewStatus">Nuevo estado del reporte.</param>
/// <param name="ManagerComment">Comentario del Manager, si lo hay.</param>
public sealed record IncidenceStatusUpdatedNotification(
    Guid IncidenceId,
    string DockCode,
    Guid ReportedByUserId,
    IncidenceStatus NewStatus,
    string? ManagerComment) : INotification;
