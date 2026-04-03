using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Reservations.Queries;

// ─── DTOs ────────────────────────────────────────────────────────

/// <summary>
/// Representación de una reserva en el historial del usuario.
/// </summary>
/// <param name="Id">Identificador de la reserva.</param>
/// <param name="DockCode">Código del puesto (ej.: N-A01).</param>
/// <param name="Date">Fecha de la reserva.</param>
/// <param name="TimeSlot">Franja horaria reservada.</param>
/// <param name="Status">Estado actual de la reserva.</param>
/// <param name="CanCancel">Indica si la reserva puede cancelarse (estado activo).</param>
public sealed record UserReservationDto(
    Guid Id,
    string DockCode,
    DateOnly Date,
    TimeSlot TimeSlot,
    ReservationStatus Status,
    bool CanCancel);

// ─── Query ───────────────────────────────────────────────────────

/// <summary>
/// Consulta que devuelve el historial completo de reservas de un usuario
/// (pasadas y futuras), incluyendo si cada reserva es cancelable.
/// </summary>
/// <param name="UserId">Identificador del usuario.</param>
public sealed record GetUserReservationsQuery(Guid UserId)
    : IRequest<IReadOnlyList<UserReservationDto>>;

// ─── Handler ─────────────────────────────────────────────────────

/// <summary>
/// Obtiene todas las reservas del usuario, ordenadas por fecha descendente,
/// y calcula si cada una puede cancelarse según las reglas de negocio.
/// </summary>
internal sealed class GetUserReservationsHandler(
    IReservationRepository repository)
    : IRequestHandler<GetUserReservationsQuery, IReadOnlyList<UserReservationDto>>
{
    public async Task<IReadOnlyList<UserReservationDto>> Handle(
        GetUserReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var reservations = await repository.GetUserReservationsAsync(request.UserId, cancellationToken);

        return reservations
            .Select(r => new UserReservationDto(
                r.Id,
                r.Dock?.Code ?? r.DockId.ToString(),
                r.Date,
                r.TimeSlot,
                r.Status,
                r.Status == ReservationStatus.Active))
            .ToList();
    }
}
