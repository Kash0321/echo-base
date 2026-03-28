namespace EchoBase.Core.Reservations;

/// <summary>
/// Códigos de error constantes para las operaciones de reserva.
/// Facilitan las aserciones en tests y el mapeo a respuestas HTTP.
/// </summary>
public static class ReservationErrors
{
    /// <summary>El puesto de trabajo solicitado no existe.</summary>
    public const string DockNotFound = "DOCK_NOT_FOUND";

    /// <summary>La fecha de la reserva es anterior al día de hoy.</summary>
    public const string DateInThePast = "DATE_IN_THE_PAST";

    /// <summary>La fecha de la reserva supera el máximo de 7 días de antelación.</summary>
    public const string DateTooFarAhead = "DATE_TOO_FAR_AHEAD";

    /// <summary>El puesto está bloqueado por un Manager para la fecha solicitada.</summary>
    public const string DockBlocked = "DOCK_BLOCKED";

    /// <summary>El puesto ya está reservado para la franja horaria solicitada en esa fecha.</summary>
    public const string DockNotAvailable = "DOCK_NOT_AVAILABLE";

    /// <summary>El usuario ha alcanzado el máximo de 2 franjas horarias por día.</summary>
    public const string UserMaxSlotsExceeded = "USER_MAX_SLOTS_EXCEEDED";

    /// <summary>El usuario ya tiene una reserva que solapa con la franja solicitada.</summary>
    public const string UserSlotConflict = "USER_SLOT_CONFLICT";

    /// <summary>La reserva indicada no existe.</summary>
    public const string ReservationNotFound = "RESERVATION_NOT_FOUND";

    /// <summary>El usuario no es el propietario de la reserva.</summary>
    public const string NotReservationOwner = "NOT_RESERVATION_OWNER";

    /// <summary>La reserva ya fue cancelada anteriormente.</summary>
    public const string AlreadyCancelled = "ALREADY_CANCELLED";

    /// <summary>La cancelación debe realizarse con al menos 24 horas de antelación.</summary>
    public const string CancellationTooLate = "CANCELLATION_TOO_LATE";
}
