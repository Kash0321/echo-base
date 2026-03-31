using EchoBase.Core.Entities;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción para leer y escribir la configuración persistida del sistema.
/// </summary>
public interface ISystemSettingRepository
{
    /// <summary>
    /// Obtiene el valor de un ajuste por su clave.
    /// Devuelve <see langword="null"/> si la clave no existe.
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Obtiene el ajuste completo por su clave, incluyendo metadatos de auditoría.
    /// Devuelve <see langword="null"/> si la clave no existe.
    /// </summary>
    Task<SystemSetting?> GetSettingAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Crea o actualiza un ajuste del sistema.
    /// </summary>
    Task SetAsync(string key, string value, Guid? updatedByUserId, DateTimeOffset updatedAt, CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
