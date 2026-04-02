namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Determina cómo se distribuyen las mesas dentro de una zona en la vista del mapa de puestos.
/// </summary>
public enum ZoneOrientation
{
    /// <summary>Las mesas se disponen de izquierda a derecha (layout en fila).</summary>
    Horizontal = 0,

    /// <summary>Las mesas se disponen de arriba a abajo (layout en columna).</summary>
    Vertical = 1
}
