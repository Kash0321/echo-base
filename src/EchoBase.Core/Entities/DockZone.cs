using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Agrupa un conjunto de puestos de trabajo bajo una misma zona física de la oficina.
/// </summary>
/// <remarks>
/// Existen dos zonas predefinidas: <c>Nostromo</c> (12 puestos en mesa corrida, 6+6)
/// y <c>Derelict</c> (12 puestos en dos mesas de 3+3 cada una).
/// </remarks>
public sealed class DockZone(Guid id) : EntityBase
{
    /// <summary>Identificador único de la zona.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>
    /// Nombre de la zona. Los valores predefinidos son <c>Nostromo</c> y <c>Derelict</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>Descripción opcional de la zona (distribución física, equipamiento común, etc.).</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Orientación visual de las mesas dentro de la zona en el mapa de puestos.
    /// <see cref="ZoneOrientation.Horizontal"/> (por defecto) muestra las mesas en fila;
    /// <see cref="ZoneOrientation.Vertical"/> las apila en columna.
    /// </summary>
    public ZoneOrientation Orientation { get; init; } = ZoneOrientation.Horizontal;

    /// <summary>Orden de visualización de la zona en el mapa de puestos. Menor valor = aparece antes.</summary>
    public int Order { get; init; }

    /// <summary>Mesas físicas configuradas en esta zona. Los puestos de trabajo se acceden a través de cada mesa.</summary>
    public ICollection<DockTable> Tables { get; } = new List<DockTable>();
}
