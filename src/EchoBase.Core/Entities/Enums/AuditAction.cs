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
}
