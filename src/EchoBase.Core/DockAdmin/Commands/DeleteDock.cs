using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para eliminar un puesto de trabajo.
/// Las reservas futuras activas asociadas al puesto serán canceladas,
/// notificando a los usuarios afectados.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la eliminación.</param>
/// <param name="DockId">Identificador del puesto a eliminar.</param>
/// <param name="Reason">Motivo de la eliminación que se incluirá en la notificación a los afectados.</param>
public sealed record DeleteDockCommand(
    Guid AdminUserId,
    Guid DockId,
    string Reason) : IRequest<Result<int>>, IAuditableRequest
{
    internal string? ResolvedDockCode { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockDeleted;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Puesto eliminado: '{ResolvedDockCode ?? DockId.ToString()}' — Motivo: {Reason}";
}

/// <summary>
/// Handler del comando <see cref="DeleteDockCommand"/>.
/// </summary>
public sealed class DeleteDockHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository,
    TimeProvider timeProvider,
    IPublisher publisher)
    : IRequestHandler<DeleteDockCommand, Result<int>>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result<int>> Handle(DeleteDockCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede eliminar puestos
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<int>.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El puesto debe existir
        var dock = await dockAdminRepository.GetDockByIdAsync(request.DockId, cancellationToken);
        if (dock is null)
            return Result<int>.Failure(DockAdminErrors.DockNotFound);

        request.ResolvedDockCode = dock.Code;

        // 3. Cancelar reservas futuras activas y notificar a los afectados
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var futureReservations = await dockAdminRepository.GetFutureActiveReservationsForDockAsync(
            request.DockId, today, cancellationToken);

        foreach (var reservation in futureReservations)
        {
            reservation.Cancel();

            // Publicar notificación de cancelación para que los handlers de email/Teams actúen
            await publisher.Publish(new ReservationCancelledNotification(
                reservation.Id,
                reservation.UserId,
                dock.Code,
                reservation.Date,
                reservation.TimeSlot), cancellationToken);
        }

        // 4. Eliminar bloqueos y reservas huérfanas, a continuación el propio puesto
        await dockAdminRepository.DeleteAllBlockedDocksForDockAsync(request.DockId, cancellationToken);
        await dockAdminRepository.DeleteAllReservationsForDockAsync(request.DockId, cancellationToken);
        await dockAdminRepository.DeleteDockAsync(dock, cancellationToken);

        return Result<int>.Success(futureReservations.Count);
    }
}
