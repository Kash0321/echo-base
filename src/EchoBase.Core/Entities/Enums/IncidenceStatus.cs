namespace EchoBase.Core.Entities.Enums;

/// <summary>
/// Estado del ciclo de vida de un reporte de incidencia.
/// </summary>
public enum IncidenceStatus
{
    /// <summary>Abierta — recién reportada, pendiente de atención por un Manager.</summary>
    Open = 1,

    /// <summary>En revisión — un Manager ha comenzado a atender la incidencia.</summary>
    UnderReview = 2,

    /// <summary>Resuelta — la incidencia ha sido corregida o atendida satisfactoriamente.</summary>
    Resolved = 3,

    /// <summary>Rechazada — la incidencia no se considera válida o no se va a atender.</summary>
    Rejected = 4
}
