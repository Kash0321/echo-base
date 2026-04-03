namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Acciones auditables registradas en el log de auditoría del sistema.
/// </summary>
public enum AuditAction
{
    /// <summary>Se creó una reserva de puesto de trabajo.</summary>
    ReservationCreated = 1,

    /// <summary>Se canceló una reserva de puesto de trabajo.</summary>
    ReservationCancelled = 2,

    /// <summary>Un Manager bloqueó uno o varios puestos de trabajo.</summary>
    DockBlocked = 3,

    /// <summary>Un Manager desbloqueó uno o varios puestos de trabajo.</summary>
    DockUnblocked = 4,

    /// <summary>Un SystemAdmin realizó una cancelación masiva de reservas.</summary>
    BulkReservationsCancelled = 5,

    /// <summary>Un SystemAdmin cambió el estado del modo de mantenimiento.</summary>
    MaintenanceModeChanged = 6,

    /// <summary>Un SystemAdmin creó una reserva de emergencia en nombre de un usuario.</summary>
    EmergencyReservationCreated = 7,

    /// <summary>Un SystemAdmin asignó un rol a un usuario.</summary>
    UserRoleAssigned = 8,

    /// <summary>Un SystemAdmin retiró un rol a un usuario.</summary>
    UserRoleRemoved = 9,

    // ── Funcionalidad 5: Configuración de zonas, mesas y puestos ──────────────

    /// <summary>Un SystemAdmin creó una nueva zona de trabajo.</summary>
    DockZoneCreated = 10,

    /// <summary>Un SystemAdmin actualizó una zona de trabajo (nombre, descripción u orientación).</summary>
    DockZoneUpdated = 11,

    /// <summary>Un SystemAdmin eliminó una zona de trabajo vacía.</summary>
    DockZoneDeleted = 12,

    /// <summary>Un SystemAdmin creó un nuevo puesto de trabajo.</summary>
    DockCreated = 13,

    /// <summary>Un SystemAdmin actualizó la información de un puesto de trabajo.</summary>
    DockUpdated = 14,

    /// <summary>Un SystemAdmin eliminó un puesto de trabajo (las reservas futuras fueron canceladas).</summary>
    DockDeleted = 15,

    /// <summary>Un SystemAdmin creó una nueva mesa lógica en una zona.</summary>
    DockTableCreated = 16,

    /// <summary>Un SystemAdmin actualizó el localizador de una mesa lógica.</summary>
    DockTableUpdated = 17,

    /// <summary>Un SystemAdmin eliminó una mesa lógica de una zona.</summary>
    DockTableDeleted = 18,

    /// <summary>Un SystemAdmin reordenó las zonas de trabajo.</summary>
    DockZonesReordered = 19,

    /// <summary>Un SystemAdmin reordenó las mesas de una zona.</summary>
    DockTablesReordered = 20,

    // ── Funcionalidad 7: Reporte de incidencias ────────────────────────────────

    /// <summary>Un usuario reportó una incidencia en un puesto de trabajo.</summary>
    IncidenceReported = 21,

    /// <summary>Un Manager actualizó el estado de un reporte de incidencia.</summary>
    IncidenceStatusUpdated = 22,
}
