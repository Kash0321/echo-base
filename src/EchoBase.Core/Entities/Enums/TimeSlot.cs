namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Representa la franja horaria en la que un empleado ocupa un puesto de trabajo.
/// </summary>
/// <remarks>
/// Según las reglas de negocio, la mañana cubre hasta las 14:00 h
/// y la tarde abarca desde las 14:00 h hasta el fin de jornada.
/// Un empleado puede reservar ambas franjas en un mismo puesto o en dos puestos distintos.
/// </remarks>
public enum TimeSlot
{
    /// <summary>Franja de mañana (inicio de jornada – 14:00 h).</summary>
    Morning = 1,

    /// <summary>Franja de tarde (14:00 h – fin de jornada).</summary>
    Afternoon = 2,

    /// <summary>Jornada completa: mañana y tarde en el mismo puesto.</summary>
    Both = 3
}
