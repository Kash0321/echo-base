namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Estado del ciclo de vida de una reserva.
/// </summary>
public enum ReservationStatus
{
    /// <summary>La reserva está vigente y el puesto está ocupado para la franja indicada.</summary>
    Active = 1,

    /// <summary>La reserva fue cancelada por el usuario o por el sistema.</summary>
    Cancelled = 2
}
