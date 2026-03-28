namespace EchoBase.Core.Entities;

/// <summary>
/// Representa un puesto de trabajo físico que puede ser reservado por los empleados.
/// </summary>
/// <remarks>
/// La capacidad total del sistema es de 24 puestos, distribuidos entre las zonas
/// <c>Nostromo</c> y <c>Derelict</c>. El código sigue el patrón <c>A-01</c>.
/// </remarks>
public sealed class Dock(Guid id) : EntityBase
{
    /// <summary>Identificador único del puesto de trabajo.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>Código alfanumérico del puesto (ej.: <c>A-01</c>).</summary>
    public required string Code { get; init; }

    /// <summary>Descripción de la ubicación física dentro de la oficina.</summary>
    public required string Location { get; init; }

    /// <summary>Equipamiento disponible en el puesto (ej.: monitor doble, silla ergonómica).</summary>
    public required string Equipment { get; init; }

    /// <summary>Identificador de la zona a la que pertenece el puesto. <see langword="null"/> si aún no está asignado.</summary>
    public Guid? DockZoneId { get; private set; }

    /// <summary>Zona a la que pertenece el puesto. <see langword="null"/> si aún no está asignado.</summary>
    public DockZone? DockZone { get; private set; }

    /// <summary>Reservas realizadas sobre este puesto.</summary>
    public ICollection<Reservation> Reservations { get; } = new List<Reservation>();

    /// <summary>
    /// Asigna el puesto a una zona de trabajo.
    /// </summary>
    /// <param name="zone">La zona a la que se asignará el puesto.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="zone"/> es <see langword="null"/>.</exception>
    public void AssignToZone(DockZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        DockZone = zone;
        DockZoneId = zone.Id;
    }

}
