using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción de acceso a datos para las operaciones CRUD de configuración
/// de zonas, mesas y puestos de trabajo realizadas por el SystemAdmin.
/// </summary>
public interface IDockAdminRepository
{
    // ── Zonas ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todas las zonas con sus puestos y mesas, para la vista de administración.
    /// </summary>
    Task<List<DockZone>> GetAllZonesWithDetailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Obtiene una zona por su identificador, incluyendo puestos y mesas.
    /// </summary>
    Task<DockZone?> GetZoneByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Comprueba si ya existe una zona con el nombre indicado,
    /// opcionalmente excluyendo la zona con <paramref name="excludeId"/>.
    /// </summary>
    Task<bool> ZoneNameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Agrega una nueva zona al contexto de persistencia.</summary>
    Task AddZoneAsync(DockZone zone, CancellationToken ct = default);

    /// <summary>
    /// Actualiza via SQL directo el nombre, descripción y orientación de una zona.
    /// </summary>
    Task UpdateZoneAsync(Guid id, string name, string? description, ZoneOrientation orientation, CancellationToken ct = default);

    /// <summary>Actualiza el campo <c>Order</c> de varias zonas en bloque (para drag-and-drop).</summary>
    Task UpdateZoneOrdersAsync(IReadOnlyList<(Guid Id, int Order)> items, CancellationToken ct = default);

    /// <summary>Actualiza el campo <c>Order</c> de varias mesas en bloque (para drag-and-drop).</summary>
    Task UpdateTableOrdersAsync(IReadOnlyList<(Guid Id, int Order)> items, CancellationToken ct = default);

    /// <summary>Elimina una zona (sin puestos) del contexto de persistencia y persiste.</summary>
    Task DeleteZoneAsync(DockZone zone, CancellationToken ct = default);

    // ── Puestos ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene un puesto por su identificador. Retorna <see langword="null"/> si no existe.
    /// </summary>
    Task<Dock?> GetDockByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Comprueba si ya existe un puesto con el código indicado,
    /// opcionalmente excluyendo el puesto con <paramref name="excludeId"/>.
    /// </summary>
    Task<bool> DockCodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Agrega un nuevo puesto al contexto de persistencia.</summary>
    Task AddDockAsync(Dock dock, CancellationToken ct = default);

    /// <summary>
    /// Actualiza via SQL directo el código, ubicación y equipamiento de un puesto.
    /// </summary>
    Task UpdateDockAsync(Guid id, string code, DockSide side, string location, string equipment, CancellationToken ct = default);

    /// <summary>
    /// Obtiene las reservas activas futuras de un puesto (desde <paramref name="fromDate"/> inclusive),
    /// con la propiedad de navegación <see cref="Reservation.User"/> cargada para notificaciones.
    /// </summary>
    Task<List<Reservation>> GetFutureActiveReservationsForDockAsync(Guid dockId, DateOnly fromDate, CancellationToken ct = default);

    /// <summary>
    /// Elimina en bloque <b>todos</b> los bloqueos administrativos asociados a un puesto.
    /// </summary>
    Task DeleteAllBlockedDocksForDockAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>
    /// Elimina en bloque <b>todas</b> las reservas (activas e históricas) de un puesto.
    /// </summary>
    Task DeleteAllReservationsForDockAsync(Guid dockId, CancellationToken ct = default);

    /// <summary>Elimina un puesto y persiste el cambio. Requiere que no queden reservas ni bloqueos asociados.</summary>
    Task DeleteDockAsync(Dock dock, CancellationToken ct = default);

    // ── Mesas ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene una mesa lógica por su identificador. Retorna <see langword="null"/> si no existe.
    /// </summary>
    Task<DockTable?> GetTableByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Comprueba si ya existe una mesa con la clave <paramref name="tableKey"/> en la zona indicada,
    /// opcionalmente excluyendo la mesa con <paramref name="excludeId"/>.
    /// </summary>
    Task<bool> TableKeyExistsInZoneAsync(string tableKey, Guid zoneId, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>Agrega una nueva mesa lógica al contexto de persistencia.</summary>
    Task AddTableAsync(DockTable table, CancellationToken ct = default);

    /// <summary>Actualiza via SQL directo la clave y localizador de una mesa lógica.</summary>
    Task UpdateTableAsync(Guid id, string tableKey, string? locator, CancellationToken ct = default);

    /// <summary>Comprueba si la mesa contiene puestos de trabajo asignados.</summary>
    Task<bool> TableHasDocksAsync(Guid tableId, CancellationToken ct = default);

    /// <summary>Elimina una mesa lógica del contexto de persistencia y persiste.</summary>
    Task DeleteTableAsync(DockTable table, CancellationToken ct = default);

    // ── Unidad de trabajo ─────────────────────────────────────────────────────

    /// <summary>Persiste los cambios pendientes en el contexto de EF Core.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
