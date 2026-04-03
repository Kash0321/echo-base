namespace EchoBase.Core.Entities;

/// <summary>
/// Configuración de sistema persistida en base de datos como pares clave-valor.
/// Permite modificar comportamientos críticos en caliente sin redespliegue.
/// </summary>
/// <remarks>
/// Clave predefinida: <c>"MaintenanceMode"</c> con valores <c>"true"</c> / <c>"false"</c>.
/// </remarks>
public sealed class SystemSetting
{
    /// <summary>Clave única del ajuste (actúa como clave primaria).</summary>
    public required string Key { get; init; }

    /// <summary>Valor del ajuste serializado como cadena.</summary>
    public required string Value { get; set; }

    /// <summary>Fecha y hora UTC de la última actualización.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Identificador del usuario que realizó la última modificación.
    /// Puede ser <see langword="null"/> si fue el sistema.
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    // ── Claves predefinidas ───────────────────────────────────────────────────

    /// <summary>Clave de la configuración del modo de mantenimiento.</summary>
    public const string MaintenanceModeKey = "MaintenanceMode";

    /// <summary>Clave del motivo del modo de mantenimiento.</summary>
    public const string MaintenanceModeReasonKey = "MaintenanceModeReason";
}
