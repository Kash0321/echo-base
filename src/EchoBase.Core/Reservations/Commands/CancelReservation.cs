using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using MediatR;

namespace EchoBase.Core.Reservations.Commands;

/// <summary>
/// Comando para cancelar una reserva existente.
/// </summary>
/// <param name="ReservationId">Identificador de la reserva a cancelar.</param>
/// <param name="UserId">Identificador del usuario que solicita la cancelación.</param>
public sealed record CancelReservationCommand(
    Guid ReservationId,
    Guid UserId) : IRequest<Result>;

/// <summary>
/// Handler que implementa las reglas de negocio para la cancelación de reservas.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>La reserva debe existir.</item>
///   <item>El solicitante debe ser el propietario de la reserva.</item>
///   <item>La reserva no puede estar ya cancelada.</item>
///   <item>La cancelación debe realizarse con al menos 24 horas de antelación.</item>
/// </list>
/// </remarks>
public sealed class CancelReservationHandler(
    IReservationRepository repository,
    TimeProvider timeProvider,
    IPublisher publisher) : IRequestHandler<CancelReservationCommand, Result>
{
    private static readonly TimeSpan MinCancellationAdvance = TimeSpan.FromHours(24);

    /// <inheritdoc />
    public async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await repository.GetByIdAsync(request.ReservationId, cancellationToken);

        if (reservation is null)
            return Result.Failure(ReservationErrors.ReservationNotFound);

        if (reservation.UserId != request.UserId)
            return Result.Failure(ReservationErrors.NotReservationOwner);

        if (reservation.Status == ReservationStatus.Cancelled)
            return Result.Failure(ReservationErrors.AlreadyCancelled);

        var now = timeProvider.GetUtcNow();
        var reservationStart = reservation.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        if (reservationStart - now < MinCancellationAdvance)
            return Result.Failure(ReservationErrors.CancellationTooLate);

        reservation.Cancel();
        await repository.SaveChangesAsync(cancellationToken);

        var dockCode = await repository.GetDockCodeAsync(reservation.DockId, cancellationToken) ?? reservation.DockId.ToString();
        await publisher.Publish(
            new ReservationCancelledNotification(
                reservation.Id, reservation.UserId, dockCode, reservation.Date, reservation.TimeSlot),
            cancellationToken);

        return Result.Success();
    }
}
