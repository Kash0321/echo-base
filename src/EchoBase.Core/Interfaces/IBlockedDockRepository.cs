using EchoBase.Core.Entities;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción de acceso a datos para operaciones de bloqueo de puestos.
/// </summary>
public interface IBlockedDockRepository
{
    /// <summary>Comprueba si un puesto está bloqueado en una fecha concreta.</summary>
    Task<bool> IsDockBlockedAsync(Guid dockId, DateOnly date, CancellationToken ct = default);

    /// <summary>Comprueba si el usuario tiene asignado un rol determinado.</summary>
    Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken ct = default);

    /// <summary>Comprueba si todos los puestos indicados existen.</summary>
    Task<bool> AllDocksExistAsync(IReadOnlyList<Guid> dockIds, CancellationToken ct = default);

    /// <summary>
    /// Obtiene los bloqueos activos que se solapan con el rango de fechas indicado
    /// para los puestos especificados.
    /// </summary>
    Task<List<BlockedDock>> GetActiveBlocksForDocksAsync(
        IReadOnlyList<Guid> dockIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);

    /// <summary>Obtiene bloqueos por sus identificadores.</summary>
    Task<List<BlockedDock>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>Agrega múltiples bloqueos al contexto de persistencia.</summary>
    Task AddRangeAsync(IEnumerable<BlockedDock> blocks, CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la base de datos.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
