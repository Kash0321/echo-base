namespace EchoBase.Core.Entities;

/// <summary>
/// Representa una mesa física dentro de una zona de trabajo.
/// </summary>
/// <remarks>
/// La estructura de mesas se infiere del patrón de código de los puestos
/// (<c>N-A01</c> → mesa <c>N</c>; <c>D-1A01</c> → mesa <c>D-1</c>), pero este entity
/// permite asociar un <see cref="Locator"/> personalizado que sustituye al texto
/// generado automáticamente en el mapa visual.
/// </remarks>
public sealed class DockTable(Guid id) : EntityBase
{
    /// <summary>Identificador único de la mesa.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>
    /// Clave de agrupación de la mesa, derivada del patrón de código de los puestos.
    /// Ejemplos: <c>"N"</c> para la mesa única de Nostromo, <c>"D-1"</c> y <c>"D-2"</c> para Derelict.
    /// </summary>
    public required string TableKey { get; init; }

    /// <summary>
    /// Texto indicativo personalizado que se muestra encima de la mesa en el mapa visual.
    /// Si es <see langword="null"/>, el UI utiliza el nombre generado automáticamente
    /// (p. ej., «Mesa 1») como fallback.
    /// </summary>
    public string? Locator { get; init; }

    /// <summary>Identificador de la zona a la que pertenece esta mesa.</summary>
    public Guid DockZoneId { get; private set; }

    /// <summary>Zona a la que pertenece esta mesa.</summary>
    public DockZone? DockZone { get; private set; }

    /// <summary>
    /// Asigna la mesa a una zona de trabajo.
    /// </summary>
    /// <param name="zone">La zona a la que se asignará la mesa.</param>
    /// <exception cref="ArgumentNullException">Se lanza cuando <paramref name="zone"/> es <see langword="null"/>.</exception>
    public void AssignToZone(DockZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        DockZoneId = zone.Id;
        DockZone = zone;
    }
}
