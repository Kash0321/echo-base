using EchoBase.Core.Entities.Enums;
using MediatR;

namespace EchoBase.Core.Reservations.Notifications;

/// <summary>
/// Notificación publicada tras crear una reserva exitosamente.
/// Los handlers de email y Teams la consumen de forma desacoplada.
/// </summary>
/// <param name="ReservationId">Identificador de la reserva creada.</param>
/// <param name="UserId">Identificador del usuario que reservó.</param>
/// <param name="DockCode">Código del puesto reservado (ej.: N-A01).</param>
/// <param name="Date">Fecha de la reserva.</param>
/// <param name="TimeSlot">Franja horaria reservada.</param>
public sealed record ReservationCreatedNotification(
    Guid ReservationId,
    Guid UserId,
    string DockCode,
    DateOnly Date,
    TimeSlot TimeSlot) : INotification;

/// <summary>
/// Notificación publicada tras cancelar una reserva exitosamente.
/// </summary>
/// <param name="ReservationId">Identificador de la reserva cancelada.</param>
/// <param name="UserId">Identificador del usuario propietario.</param>
/// <param name="DockCode">Código del puesto que se liberó.</param>
/// <param name="Date">Fecha de la reserva cancelada.</param>
/// <param name="TimeSlot">Franja horaria liberada.</param>
public sealed record ReservationCancelledNotification(
    Guid ReservationId,
    Guid UserId,
    string DockCode,
    DateOnly Date,
    TimeSlot TimeSlot) : INotification;
