using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Commands;

/// <summary>
/// Resultado de una operación de cancelación masiva de reservas.
/// </summary>
/// <param name="CancelledCount">Número de reservas canceladas.</param>
public sealed record BulkCancelResult(int CancelledCount);

/// <summary>
/// Comando para cancelar en masa reservas dentro de un rango de fechas.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la cancelación.</param>
/// <param name="StartDate">Fecha de inicio del rango a cancelar (inclusiva).</param>
/// <param name="EndDate">Fecha de fin del rango a cancelar (inclusiva).</param>
/// <param name="Reason">Motivo de la cancelación masiva (se envía en las notificaciones).</param>
/// <param name="DockIds">
/// Si se proporciona, solo cancela reservas de los puestos indicados.
/// Si es <see langword="null"/> o vacío, cancela todas las reservas del rango.
/// </param>
public sealed record BulkCancelReservationsCommand(
    Guid AdminUserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    IReadOnlyList<Guid>? DockIds) : IRequest<Result<BulkCancelResult>>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.BulkReservationsCancelled;
    string IAuditableRequest.BuildAuditDetails()
    {
        var scope = (DockIds is { Count: > 0 })
            ? $"{DockIds.Count} puesto(s) específico(s)"
            : "todos los puestos";
        return $"Cancelación masiva del {StartDate:dd/MM/yyyy} al {EndDate:dd/MM/yyyy} en {scope}. Motivo: {Reason}";
    }
}

/// <summary>
/// Handler del comando <see cref="BulkCancelReservationsCommand"/>.
/// </summary>
public sealed class BulkCancelReservationsHandler(
    IBlockedDockRepository blockedDockRepository,
    IReservationRepository reservationRepository,
    IPublisher publisher)
    : IRequestHandler<BulkCancelReservationsCommand, Result<BulkCancelResult>>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result<BulkCancelResult>> Handle(
        BulkCancelReservationsCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede ejecutar esta acción
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<BulkCancelResult>.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. Validar rango de fechas
        if (request.EndDate < request.StartDate)
            return Result<BulkCancelResult>.Failure(SystemAdminErrors.InvalidDateRange);

        // 3. Obtener todas las reservas activas en el rango (y opcionalmente por puestos)
        var reservations = await reservationRepository.GetActiveReservationsInRangeAsync(
            request.StartDate,
            request.EndDate,
            request.DockIds,
            cancellationToken);

        // 4. Cancelar cada reserva y publicar notificación individual
        foreach (var reservation in reservations)
        {
            reservation.Cancel();

            var dockCode = reservation.Dock?.Code ?? reservation.DockId.ToString();
            await publisher.Publish(
                new ReservationCancelledNotification(
                    reservation.Id,
                    reservation.UserId,
                    dockCode,
                    reservation.Date,
                    reservation.TimeSlot),
                cancellationToken);
        }

        if (reservations.Count > 0)
            await reservationRepository.SaveChangesAsync(cancellationToken);

        return Result<BulkCancelResult>.Success(new BulkCancelResult(reservations.Count));
    }
}
