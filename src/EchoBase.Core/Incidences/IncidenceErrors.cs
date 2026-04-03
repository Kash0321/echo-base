namespace EchoBase.Core.Incidences;

/// <summary>
/// Códigos de error para la funcionalidad de reporte de incidencias.
/// </summary>
public static class IncidenceErrors
{
    /// <summary>El puesto de trabajo especificado no existe.</summary>
    public const string DockNotFound = "INCIDENCE_DOCK_NOT_FOUND";

    /// <summary>La descripción de la incidencia es obligatoria y no puede estar vacía.</summary>
    public const string DescriptionRequired = "INCIDENCE_DESCRIPTION_REQUIRED";

    /// <summary>El reporte de incidencia especificado no existe.</summary>
    public const string IncidenceNotFound = "INCIDENCE_NOT_FOUND";

    /// <summary>El usuario no tiene el rol de Manager necesario para esta operación.</summary>
    public const string NotManager = "INCIDENCE_NOT_MANAGER";
}
