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

    /// <summary>
    /// Obtiene todas las reservas activas de los puestos indicados que se solapan con el rango de fechas especificado.
    /// Carga la propiedad de navegación <see cref="Reservation.Dock"/> para obtener el código del puesto.
    /// Se usa al bloquear puestos para cancelar automáticamente las reservas conflictivas.
    /// </summary>
    Task<List<Reservation>> GetActiveReservationsForDocksInRangeAsync(
        IReadOnlyList<Guid> dockIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la base de datos.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las reservas activas cuya fecha cae dentro del rango especificado,
    /// opcionalmente limitadas a los puestos indicados.
    /// Carga las propiedades de navegación <see cref="Reservation.User"/> y <see cref="Reservation.Dock"/>.
    /// Se usa para la cancelación masiva de reservas por el SystemAdmin.
    /// </summary>
    /// <param name="startDate">Fecha de inicio del rango (inclusiva).</param>
    /// <param name="endDate">Fecha de fin del rango (inclusiva).</param>
    /// <param name="dockIds">
    /// Si se proporciona, solo devuelve reservas de esos puestos concretos.
    /// Si es <see langword="null"/> o vacío, devuelve reservas de todos los puestos.
    /// </param>
    Task<List<Reservation>> GetActiveReservationsInRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<Guid>? dockIds,
        CancellationToken ct = default);
}
