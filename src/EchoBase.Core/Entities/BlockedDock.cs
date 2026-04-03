namespace EchoBase.Core.Entities;

/// <summary>
/// Representa el bloqueo de un puesto de trabajo por parte de un Manager
/// durante un período de uno o varios días, impidiendo su reserva.
/// </summary>
/// <remarks>
/// Mientras un bloqueo esté activo, los usuarios con rol <c>BasicUser</c>
/// no podrán reservar el puesto afectado para las fechas comprendidas
/// entre <see cref="StartDate"/> y <see cref="EndDate"/> (ambas inclusivas).
/// </remarks>
public sealed class BlockedDock(
    Guid id,
    Guid dockId,
    Guid blockedByUserId,
    DateOnly startDate,
    DateOnly endDate,
    string reason) : EntityBase
{
    /// <summary>Identificador único del bloqueo.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>Identificador del puesto de trabajo bloqueado.</summary>
    public Guid DockId { get; } = EnsureValidId(dockId, nameof(dockId));

    /// <summary>Identificador del Manager que creó el bloqueo.</summary>
    public Guid BlockedByUserId { get; } = EnsureValidId(blockedByUserId, nameof(blockedByUserId));

    /// <summary>Fecha de inicio del bloqueo (inclusiva).</summary>
    public DateOnly StartDate { get; } = startDate;

    /// <summary>Fecha de fin del bloqueo (inclusiva).</summary>
    public DateOnly EndDate { get; } = endDate;

    /// <summary>Motivo del bloqueo.</summary>
    public string Reason { get; } = reason ?? throw new ArgumentNullException(nameof(reason));

    /// <summary>Indica si el bloqueo está activo. Un bloqueo desactivado ya no impide reservas.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Puesto de trabajo bloqueado. Se carga mediante navegación de EF Core.</summary>
    public Dock? Dock { get; private set; }

    /// <summary>Manager que creó el bloqueo. Se carga mediante navegación de EF Core.</summary>
    public User? BlockedByUser { get; private set; }

    /// <summary>Desactiva el bloqueo, permitiendo de nuevo la reserva del puesto.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Establece la referencia de navegación al Manager que creó el bloqueo.</summary>
    /// <param name="user">Instancia del usuario Manager.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="user"/> es <see langword="null"/>.</exception>
    public void SetBlockedByUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        BlockedByUser = user;
    }
}
