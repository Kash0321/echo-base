using EchoBase.Core.Entities.Enums;
using MediatR;

namespace EchoBase.Core.Reservations.Notifications;

/// <summary>
/// Notificación publicada por el servicio de recordatorios automáticos
/// para avisar a un usuario de que tiene una reserva próxima.
/// Los handlers de email y Teams la consumen de forma desacoplada.
/// </summary>
/// <param name="ReservationId">Identificador de la reserva.</param>
/// <param name="UserId">Identificador del usuario propietario.</param>
/// <param name="DockCode">Código del puesto reservado (ej.: N-A01).</param>
/// <param name="Date">Fecha de la reserva.</param>
/// <param name="TimeSlot">Franja horaria reservada.</param>
public sealed record ReservationReminderNotification(
    Guid ReservationId,
    Guid UserId,
    string DockCode,
    DateOnly Date,
    TimeSlot TimeSlot) : INotification;
