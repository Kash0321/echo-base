using EchoBase.Core.Entities;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción de acceso a datos para la visualización del mapa de puestos.
/// </summary>
public interface IDockMapRepository
{
    /// <summary>Obtiene todas las zonas con sus puestos de trabajo.</summary>
    Task<List<DockZone>> GetAllZonesWithDocksAsync(CancellationToken ct = default);

    /// <summary>Obtiene todas las reservas activas para una fecha concreta, incluyendo el usuario que las realizó.</summary>
    Task<List<Reservation>> GetAllActiveReservationsForDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Obtiene los bloqueos activos para una fecha concreta, incluyendo el Manager que los creó.</summary>
    Task<List<BlockedDock>> GetBlockedDocksForDateAsync(DateOnly date, CancellationToken ct = default);
}
