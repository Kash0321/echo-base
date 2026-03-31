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
    Guid UserId) : IRequest<Result>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => UserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.ReservationCancelled;
    string IAuditableRequest.BuildAuditDetails() => $"Cancelación de reserva {ReservationId}";
}

/// <summary>
/// Handler que implementa las reglas de negocio para la cancelación de reservas.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>La reserva debe existir.</item>
///   <item>El solicitante debe ser el propietario de la reserva.</item>
///   <item>La reserva no puede estar ya cancelada.</item>
/// </list>
/// </remarks>
public sealed class CancelReservationHandler(
    IReservationRepository repository,
    IPublisher publisher) : IRequestHandler<CancelReservationCommand, Result>
{
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
