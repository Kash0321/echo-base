namespace EchoBase.Core.DockAdmin;

/// <summary>
/// Constantes de error para las operaciones de configuración de zonas, mesas y puestos.
/// </summary>
public static class DockAdminErrors
{
    // ── Zonas ─────────────────────────────────────────────────────────────────

    /// <summary>La zona de trabajo especificada no existe.</summary>
    public const string ZoneNotFound = "ZONE_NOT_FOUND";

    /// <summary>Ya existe una zona con el mismo nombre.</summary>
    public const string ZoneNameAlreadyExists = "ZONE_NAME_ALREADY_EXISTS";

    /// <summary>No se puede eliminar la zona porque contiene puestos asignados.</summary>
    public const string ZoneHasDocks = "ZONE_HAS_DOCKS";

    // ── Puestos ───────────────────────────────────────────────────────────────

    /// <summary>El puesto de trabajo especificado no existe.</summary>
    public const string DockNotFound = "DOCK_NOT_FOUND";

    /// <summary>Ya existe un puesto con el mismo código.</summary>
    public const string DockCodeAlreadyExists = "DOCK_CODE_ALREADY_EXISTS";

    /// <summary>El código del puesto no puede estar vacío.</summary>
    public const string DockCodeRequired = "DOCK_CODE_REQUIRED";

    // ── Mesas ─────────────────────────────────────────────────────────────────

    /// <summary>La mesa lógica especificada no existe.</summary>
    public const string TableNotFound = "TABLE_NOT_FOUND";

    /// <summary>Ya existe una mesa con la misma clave en la zona indicada.</summary>
    public const string TableKeyAlreadyExists = "TABLE_KEY_ALREADY_EXISTS";

    /// <summary>No se puede eliminar la mesa porque contiene puestos asignados.</summary>
    public const string TableHasDocks = "TABLE_HAS_DOCKS";

    /// <summary>La clave de mesa no puede estar vacía.</summary>
    public const string TableKeyRequired = "TABLE_KEY_REQUIRED";
}
