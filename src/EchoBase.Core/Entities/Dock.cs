using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Representa un puesto de trabajo físico que puede ser reservado por los empleados.
/// </summary>
/// <remarks>
/// La capacidad total del sistema es de 24 puestos, distribuidos entre las zonas
/// <c>Nostromo</c> y <c>Derelict</c>. El código sigue el patrón <c>A-01</c>.
/// Cada puesto pertenece a una <see cref="DockTable"/> (mesa física) que a su vez forma parte de una <see cref="DockZone"/>.
/// </remarks>
public sealed class Dock(Guid id) : EntityBase
{
    /// <summary>Identificador único del puesto de trabajo.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>Código alfanumérico del puesto (ej.: <c>N-A01</c>).</summary>
    public required string Code { get; init; }

    /// <summary>Descripción de la ubicación física dentro de la oficina.</summary>
    public required string Location { get; init; }

    /// <summary>Equipamiento disponible en el puesto (ej.: monitor doble, silla ergonómica).</summary>
    public required string Equipment { get; init; }

    /// <summary>Identificador de la mesa física a la que pertenece el puesto.</summary>
    public Guid DockTableId { get; private set; }

    /// <summary>Mesa física a la que pertenece el puesto.</summary>
    public DockTable? DockTable { get; private set; }

    /// <summary>Lado de la mesa al que pertenece el puesto (A o B).</summary>
    public DockSide Side { get; private set; }

    /// <summary>Reservas realizadas sobre este puesto.</summary>
    public ICollection<Reservation> Reservations { get; } = new List<Reservation>();

    /// <summary>
    /// Asigna el puesto a una mesa física y establece su lado (A o B).
    /// </summary>
    /// <param name="table">La mesa a la que se asignará el puesto.</param>
    /// <param name="side">El lado de la mesa al que pertenece el puesto.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="table"/> es <see langword="null"/>.</exception>
    public void AssignToTable(DockTable table, DockSide side)
    {
        ArgumentNullException.ThrowIfNull(table);
        DockTable = table;
        DockTableId = table.Id;
        Side = side;
    }
}
