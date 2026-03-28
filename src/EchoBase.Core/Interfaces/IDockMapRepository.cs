using EchoBase.Core.Entities;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción de acceso a datos para la visualización del mapa de puestos.
/// </summary>
public interface IDockMapRepository
{
    /// <summary>Obtiene todas las zonas con sus puestos de trabajo.</summary>
    Task<List<DockZone>> GetAllZonesWithDocksAsync(CancellationToken ct = default);

    /// <summary>Obtiene todas las reservas activas para una fecha concreta.</summary>
    Task<List<Reservation>> GetAllActiveReservationsForDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Obtiene los identificadores de puestos bloqueados en una fecha concreta.</summary>
    Task<List<Guid>> GetBlockedDockIdsForDateAsync(DateOnly date, CancellationToken ct = default);
}
