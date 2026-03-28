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

    /// <summary>Puestos de trabajo que pertenecen a esta zona.</summary>
    public ICollection<Dock> Docks { get; } = new List<Dock>();

}
