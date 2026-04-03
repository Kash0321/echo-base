using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Representa la reserva de un puesto de trabajo para un empleado en una fecha y franja horaria concretas.
/// </summary>
/// <remarks>
/// Reglas de negocio relevantes:
/// <list type="bullet">
///   <item>Un empleado puede realizar como máximo dos reservas al día (una por franja).</item>
///   <item>Las reservas solo pueden crearse con hasta 7 días de antelación.</item>
///   <item>Las reservas activas pueden cancelarse en cualquier momento sin restricción de antelación.</item>
/// </list>
/// </remarks>
public sealed class Reservation(
    Guid id,
    Guid userId,
    Guid dockId,
    DateOnly date,
    TimeSlot timeSlot) : EntityBase
{
    /// <summary>Identificador único de la reserva.</summary>
    public Guid Id { get; } = EnsureValidId(id, nameof(id));

    /// <summary>Identificador del usuario que realizó la reserva.</summary>
    public Guid UserId { get; } = EnsureValidId(userId, nameof(userId));

    /// <summary>Identificador del puesto de trabajo reservado.</summary>
    public Guid DockId { get; } = EnsureValidId(dockId, nameof(dockId));

    /// <summary>Fecha de la reserva (sin componente horario).</summary>
    public DateOnly Date { get; } = date;

    /// <summary>Franja horaria para la que se ha realizado la reserva.</summary>
    public TimeSlot TimeSlot { get; private set; } = timeSlot;

    /// <summary>Estado actual de la reserva.</summary>
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;

    /// <summary>Usuario propietario de la reserva. Se carga mediante navegación de EF Core.</summary>
    public User? User { get; private set; }

    /// <summary>Puesto de trabajo reservado. Se carga mediante navegación de EF Core.</summary>
    public Dock? Dock { get; private set; }

    /// <summary>Cancela la reserva estableciendo su estado a <see cref="ReservationStatus.Cancelled"/>.</summary>
    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
    }

    /// <summary>Reactiva una reserva cancelada estableciendo su estado a <see cref="ReservationStatus.Active"/>.</summary>
    public void Activate()
    {
        Status = ReservationStatus.Active;
    }

    /// <summary>
    /// Modifica la franja horaria de la reserva.
    /// </summary>
    /// <param name="timeSlot">Nueva franja horaria.</param>
    public void ChangeTimeSlot(TimeSlot timeSlot)
    {
        TimeSlot = timeSlot;
    }

    /// <summary>Establece la referencia de navegación al usuario propietario.</summary>
    /// <param name="user">Instancia del usuario.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="user"/> es <see langword="null"/>.</exception>
    public void SetUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        User = user;
    }

    /// <summary>Establece la referencia de navegación al puesto de trabajo.</summary>
    /// <param name="dock">Instancia del puesto de trabajo.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="dock"/> es <see langword="null"/>.</exception>
    public void SetDock(Dock dock)
    {
        ArgumentNullException.ThrowIfNull(dock);

        Dock = dock;
    }

}
