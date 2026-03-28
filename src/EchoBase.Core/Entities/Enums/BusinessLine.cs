namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Línea de negocio a la que pertenece un empleado.
/// </summary>
public enum BusinessLine
{
    /// <summary>Área Core de la compañía.</summary>
    Core = 1,

    /// <summary>Área de Energía.</summary>
    Energia = 2,

    /// <summary>Área de Scrap / Waste (residuos).</summary>
    ScrapWaste = 3,

    /// <summary>Área Transversal (servicios compartidos).</summary>
    Transversal = 4
}
