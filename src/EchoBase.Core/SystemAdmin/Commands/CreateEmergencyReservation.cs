using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Notifications;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Commands;

/// <summary>
/// Comando para crear una reserva de emergencia en nombre de un usuario.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <remarks>
/// Esta operación aplica las mismas validaciones de negocio que la reserva normal,
/// sin excepción alguna. La diferencia es que el SystemAdmin la realiza en nombre de otro usuario.
/// </remarks>
/// <param name="AdminUserId">Identificador del SystemAdmin que crea la reserva.</param>
/// <param name="TargetUserId">Identificador del usuario en cuyo nombre se realiza la reserva.</param>
/// <param name="DockId">Identificador del puesto a reservar.</param>
/// <param name="Date">Fecha de la reserva.</param>
/// <param name="TimeSlot">Franja horaria solicitada.</param>
public sealed record CreateEmergencyReservationCommand(
    Guid AdminUserId,
    Guid TargetUserId,
    Guid DockId,
    DateOnly Date,
    TimeSlot TimeSlot) : IRequest<Result<Guid>>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.EmergencyReservationCreated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Reserva de emergencia en puesto {DockId} para usuario {TargetUserId} el {Date:dd/MM/yyyy}, franja {TimeSlot}";
}

/// <summary>
/// Handler del comando <see cref="CreateEmergencyReservationCommand"/>.
/// </summary>
public sealed class CreateEmergencyReservationHandler(
    IBlockedDockRepository blockedDockRepository,
    IReservationRepository reservationRepository,
    IPublisher publisher,
    TimeProvider timeProvider)
    : IRequestHandler<CreateEmergencyReservationCommand, Result<Guid>>
{
    private const string SystemAdminRole = "SystemAdmin";
    private const int MaxAdvanceDays = 7;
    private const int MaxDailySlots = 2;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        CreateEmergencyReservationCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede ejecutar esta acción
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<Guid>.Failure(SystemAdminErrors.NotSystemAdmin);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // 2. La fecha no puede ser anterior a hoy (misma validación que reserva normal)
        if (request.Date < today)
            return Result<Guid>.Failure(ReservationErrors.DateInThePast);

        // 3. La fecha no puede superar los 7 días de antelación (misma validación)
        if (request.Date > today.AddDays(MaxAdvanceDays))
            return Result<Guid>.Failure(ReservationErrors.DateTooFarAhead);

        // 4. El puesto debe existir
        if (!await reservationRepository.DockExistsAsync(request.DockId, cancellationToken))
            return Result<Guid>.Failure(ReservationErrors.DockNotFound);

        // 5. El puesto no puede estar bloqueado
        if (await blockedDockRepository.IsDockBlockedAsync(request.DockId, request.Date, cancellationToken))
            return Result<Guid>.Failure(ReservationErrors.DockBlocked);

        // 6. Disponibilidad del puesto en la franja
        var dockReservations = await reservationRepository.GetActiveDockReservationsAsync(
            request.DockId, request.Date, cancellationToken);

        if (dockReservations.Exists(r => TimeSlotsOverlap(r.TimeSlot, request.TimeSlot)))
            return Result<Guid>.Failure(ReservationErrors.DockNotAvailable);

        // 7. Límite diario del usuario destino
        var userReservations = await reservationRepository.GetActiveUserReservationsAsync(
            request.TargetUserId, request.Date, cancellationToken);

        int currentSlots = userReservations.Sum(r => SlotCount(r.TimeSlot));
        int newSlots = SlotCount(request.TimeSlot);

        if (currentSlots + newSlots > MaxDailySlots)
            return Result<Guid>.Failure(ReservationErrors.UserMaxSlotsExceeded);

        if (userReservations.Exists(r => TimeSlotsOverlap(r.TimeSlot, request.TimeSlot)))
            return Result<Guid>.Failure(ReservationErrors.UserSlotConflict);

        // 8. Crear la reserva en nombre del usuario destino
        var reservation = new Reservation(
            Guid.NewGuid(),
            request.TargetUserId,
            request.DockId,
            request.Date,
            request.TimeSlot);

        await reservationRepository.AddAsync(reservation, cancellationToken);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        // Notificar al usuario destino
        var dockCode = await reservationRepository.GetDockCodeAsync(request.DockId, cancellationToken)
                       ?? request.DockId.ToString();
        await publisher.Publish(
            new ReservationCreatedNotification(
                reservation.Id, request.TargetUserId, dockCode, request.Date, request.TimeSlot),
            cancellationToken);

        return Result<Guid>.Success(reservation.Id);
    }

    private static bool TimeSlotsOverlap(TimeSlot a, TimeSlot b) =>
        a == TimeSlot.Both || b == TimeSlot.Both || a == b;

    private static int SlotCount(TimeSlot slot) =>
        slot == TimeSlot.Both ? 2 : 1;
}
