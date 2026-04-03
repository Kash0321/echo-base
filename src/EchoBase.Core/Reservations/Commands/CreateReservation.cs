using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Notifications;
using MediatR;

namespace EchoBase.Core.Reservations.Commands;

/// <summary>
/// Comando para crear una nueva reserva de puesto de trabajo.
/// </summary>
/// <param name="UserId">Identificador del empleado que realiza la reserva.</param>
/// <param name="DockId">Identificador del puesto a reservar.</param>
/// <param name="Date">Fecha de la reserva (solo fecha, sin hora).</param>
/// <param name="TimeSlot">Franja horaria solicitada.</param>
public sealed record CreateReservationCommand(
    Guid UserId,
    Guid DockId,
    DateOnly Date,
    TimeSlot TimeSlot) : IRequest<Result<Guid>>, IAuditableRequest
{
    internal string? ResolvedDockCode { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => UserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.ReservationCreated;
    string IAuditableRequest.BuildAuditDetails()
    {
        var slot = TimeSlot switch
        {
            TimeSlot.Morning   => "Mañana",
            TimeSlot.Afternoon => "Tarde",
            TimeSlot.Both      => "Mañana y Tarde",
            _                  => TimeSlot.ToString()
        };
        return $"Puesto {ResolvedDockCode ?? DockId.ToString()} · {Date:dd/MM/yyyy} · {slot}";
    }
}

/// <summary>
/// Handler que implementa las reglas de negocio para la creación de reservas.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>La fecha no puede ser pasada.</item>
///   <item>La fecha no puede superar los 7 días de antelación.</item>
///   <item>El puesto debe existir.</item>
///   <item>El puesto debe estar disponible para la franja solicitada.</item>
///   <item>El usuario no puede exceder 2 franjas diarias en total.</item>
///   <item>El usuario no puede tener franjas solapadas con la solicitada.</item>
/// </list>
/// </remarks>
public sealed class CreateReservationHandler(
    IReservationRepository repository,
    IBlockedDockRepository blockedDockRepository,
    TimeProvider timeProvider,
    IPublisher publisher) : IRequestHandler<CreateReservationCommand, Result<Guid>>
{
    private const int MaxAdvanceDays = 7;
    private const int MaxDailySlots = 2;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // 1. La fecha no puede ser anterior a hoy
        if (request.Date < today)
            return Result<Guid>.Failure(ReservationErrors.DateInThePast);

        // 2. La fecha no puede superar los 7 días de antelación
        if (request.Date > today.AddDays(MaxAdvanceDays))
            return Result<Guid>.Failure(ReservationErrors.DateTooFarAhead);

        // 3. El puesto debe existir
        if (!await repository.DockExistsAsync(request.DockId, cancellationToken))
            return Result<Guid>.Failure(ReservationErrors.DockNotFound);

        // 3b. El puesto no puede estar bloqueado para la fecha solicitada
        if (await blockedDockRepository.IsDockBlockedAsync(request.DockId, request.Date, cancellationToken))
            return Result<Guid>.Failure(ReservationErrors.DockBlocked);

        // 4. Disponibilidad del puesto: no puede haber franjas solapadas en la misma fecha
        var dockReservations = await repository.GetActiveDockReservationsAsync(
            request.DockId, request.Date, cancellationToken);

        if (dockReservations.Exists(r => TimeSlotsOverlap(r.TimeSlot, request.TimeSlot)))
            return Result<Guid>.Failure(ReservationErrors.DockNotAvailable);

        // 5. Límite diario del usuario
        var userReservations = await repository.GetActiveUserReservationsAsync(
            request.UserId, request.Date, cancellationToken);

        // 5a. El total de franjas (existentes + solicitada) no puede superar el máximo
        int currentSlots = userReservations.Sum(r => SlotCount(r.TimeSlot));
        int newSlots = SlotCount(request.TimeSlot);

        if (currentSlots + newSlots > MaxDailySlots)
            return Result<Guid>.Failure(ReservationErrors.UserMaxSlotsExceeded);

        // 5b. No puede tener franjas que solapen con la solicitada
        if (userReservations.Exists(r => TimeSlotsOverlap(r.TimeSlot, request.TimeSlot)))
            return Result<Guid>.Failure(ReservationErrors.UserSlotConflict);

        // 6. Crear la reserva
        var reservation = new Reservation(
            Guid.CreateVersion7(),
            request.UserId,
            request.DockId,
            request.Date,
            request.TimeSlot);

        await repository.AddAsync(reservation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Publicar notificación para email y Teams (fire-and-forget dentro del scope)
        var dockCode = await repository.GetDockCodeAsync(request.DockId, cancellationToken) ?? request.DockId.ToString();
        request.ResolvedDockCode = dockCode;
        await publisher.Publish(
            new ReservationCreatedNotification(
                reservation.Id, request.UserId, dockCode, request.Date, request.TimeSlot),
            cancellationToken);

        return Result<Guid>.Success(reservation.Id);
    }

    /// <summary>Determina si dos franjas horarias se solapan.</summary>
    internal static bool TimeSlotsOverlap(TimeSlot a, TimeSlot b) =>
        a == TimeSlot.Both || b == TimeSlot.Both || a == b;

    /// <summary>Cantidad de franjas atómicas que ocupa un <see cref="TimeSlot"/>.</summary>
    internal static int SlotCount(TimeSlot slot) =>
        slot == TimeSlot.Both ? 2 : 1;
}
