using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción de acceso a datos para operaciones de reserva.
/// Permite desacoplar la lógica de negocio de la implementación de persistencia.
/// </summary>
public interface IReservationRepository
{
    /// <summary>Comprueba si existe un puesto de trabajo con el identificador indicado.</summary>
    Task<bool> DockExistsAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las reservas activas de un puesto para una fecha concreta.
    /// Se usa para validar la disponibilidad del puesto en la franja solicitada.
    /// </summary>
    Task<List<Reservation>> GetActiveDockReservationsAsync(Guid dockId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las reservas activas de un usuario para una fecha concreta.
    /// Se usa para validar el límite de franjas diarias y posibles conflictos de franja.
    /// </summary>
    Task<List<Reservation>> GetActiveUserReservationsAsync(Guid userId, DateOnly date, CancellationToken ct = default);

    /// <summary>Recupera una reserva por su identificador.</summary>
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Agrega una nueva reserva al contexto de persistencia.</summary>
    Task AddAsync(Reservation reservation, CancellationToken ct = default);

    /// <summary>Obtiene el código de un puesto de trabajo por su identificador.</summary>
    Task<string?> GetDockCodeAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las reservas de un usuario, incluyendo las propiedades de navegación Dock,
    /// ordenadas por fecha descendente.
    /// </summary>
    Task<List<Reservation>> GetUserReservationsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las reservas activas cuya fecha coincide con la indicada,
    /// incluyendo las propiedades de navegación Dock y User.
    /// Se usa para enviar recordatorios automáticos.
    /// </summary>
    Task<List<Reservation>> GetActiveReservationsForDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la base de datos.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
