using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Reservations.Queries;

// ─── DTOs ────────────────────────────────────────────────────────

/// <summary>
/// Estado de un puesto de trabajo para una fecha concreta.
/// </summary>
public enum DockStatus
{
    /// <summary>Puesto libre en ambas franjas.</summary>
    Free = 0,

    /// <summary>Puesto con una franja ocupada y la otra libre.</summary>
    PartiallyBooked = 1,

    /// <summary>Puesto con ambas franjas ocupadas.</summary>
    FullyBooked = 2,

    /// <summary>Puesto bloqueado por un Manager.</summary>
    Blocked = 3
}

/// <summary>
/// Representación de un puesto individual en el mapa visual.
/// </summary>
/// <param name="Id">Identificador del puesto.</param>
/// <param name="Code">Código del puesto (ej.: N-A01).</param>
/// <param name="Status">Estado del puesto para la fecha consultada.</param>
/// <param name="BookedSlot">Franja reservada (solo si <see cref="Status"/> es <see cref="DockStatus.PartiallyBooked"/>).</param>
/// <param name="MorningBookedBy">Nombre del usuario que ha reservado la franja de mañana (o ambas). <see langword="null"/> si la franja está libre.</param>
/// <param name="AfternoonBookedBy">Nombre del usuario que ha reservado la franja de tarde (o ambas). <see langword="null"/> si la franja está libre.</param>
/// <param name="BlockedByName">Nombre del Manager que bloqueó el puesto. Solo presente cuando <see cref="Status"/> es <see cref="DockStatus.Blocked"/>.</param>
/// <param name="BlockReason">Motivo del bloqueo. Solo presente cuando <see cref="Status"/> es <see cref="DockStatus.Blocked"/>.</param>
/// <param name="IncidenceCounts">Conteo de incidencias por estado para este puesto.</param>
public sealed record DockSeatDto(
    Guid Id,
    string Code,
    DockStatus Status,
    TimeSlot? BookedSlot,
    string? MorningBookedBy = null,
    string? AfternoonBookedBy = null,
    string? BlockedByName = null,
    string? BlockReason = null,
    IReadOnlyDictionary<IncidenceStatus, int>? IncidenceCounts = null);

/// <summary>
/// Representación de una mesa física con dos lados (A y B).
/// </summary>
/// <param name="Name">Nombre descriptivo generado automáticamente (ej.: «Mesa 1», «Nostromo»).</param>
/// <param name="Locator">Texto indicativo personalizado que sustituye a <see cref="Name"/> en el mapa visual. Si es <see langword="null"/>, el UI muestra <see cref="Name"/> como fallback.</param>
/// <param name="SideA">Puestos del lado A.</param>
/// <param name="SideB">Puestos del lado B.</param>
public sealed record DockTableDto(
    string Name,
    string? Locator,
    IReadOnlyList<DockSeatDto> SideA,
    IReadOnlyList<DockSeatDto> SideB);

/// <summary>
/// Representación de una zona en el mapa de puestos.
/// </summary>
/// <param name="Id">Identificador de la zona.</param>
/// <param name="Name">Nombre de la zona (Nostromo, Derelict).</param>
/// <param name="Description">Descripción opcional.</param>
/// <param name="Orientation">Orientación visual de las mesas dentro de la zona: horizontal (en fila) o vertical (en columna).</param>
/// <param name="Tables">Mesas que componen la zona.</param>
public sealed record DockZoneMapDto(
    Guid Id,
    string Name,
    string? Description,
    ZoneOrientation Orientation,
    IReadOnlyList<DockTableDto> Tables);

/// <summary>
/// Resultado completo del mapa de puestos para una fecha.
/// </summary>
/// <param name="Date">Fecha consultada.</param>
/// <param name="Zones">Zonas con sus mesas y puestos.</param>
public sealed record DockMapDto(
    DateOnly Date,
    IReadOnlyList<DockZoneMapDto> Zones);

// ─── Query ───────────────────────────────────────────────────────

/// <summary>
/// Consulta para obtener el mapa completo de puestos con su estado para una fecha.
/// </summary>
/// <param name="Date">Fecha para la que se consulta el mapa.</param>
public sealed record GetDockMapQuery(DateOnly Date) : IRequest<DockMapDto>;

// ─── Handler ─────────────────────────────────────────────────────

/// <summary>
/// Obtiene el estado de todos los puestos de trabajo agrupados por zona y mesa.
/// </summary>
/// <remarks>
/// La estructura de mesas se obtiene directamente de la jerarquía <see cref="Entities.DockZone"/>
/// → <see cref="Entities.DockTable"/> → <see cref="Entities.Dock"/>. El lado (A/B) de cada
/// puesto se lee de la propiedad <see cref="Entities.Dock.Side"/>.
/// </remarks>
internal sealed class GetDockMapHandler(IDockMapRepository repository, IIncidenceRepository incidenceRepository)
    : IRequestHandler<GetDockMapQuery, DockMapDto>
{
    public async Task<DockMapDto> Handle(GetDockMapQuery request, CancellationToken cancellationToken)
    {
        var zones = await repository.GetAllZonesWithDocksAsync(cancellationToken);
        var reservations = await repository.GetAllActiveReservationsForDateAsync(request.Date, cancellationToken);
        var blockedDocks = await repository.GetBlockedDocksForDateAsync(request.Date, cancellationToken);
        var incidenceCounts = await incidenceRepository.GetIncidenceCountsByDockAsync(cancellationToken);

        var blockedMap = blockedDocks.ToDictionary(b => b.DockId);
        var reservationsByDock = reservations
            .GroupBy(r => r.DockId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var zoneDtos = zones
            .OrderBy(z => z.Order).ThenBy(z => z.Name)
            .Select(z => BuildZoneDto(z, blockedMap, reservationsByDock, incidenceCounts))
            .ToList();

        return new DockMapDto(request.Date, zoneDtos);
    }

    internal static DockZoneMapDto BuildZoneDto(
        Entities.DockZone zone,
        Dictionary<Guid, Entities.BlockedDock> blockedMap,
        Dictionary<Guid, List<Entities.Reservation>> reservationsByDock,
        Dictionary<Guid, Dictionary<IncidenceStatus, int>> incidenceCounts)
    {
        var tables = zone.Tables
            .OrderBy(t => t.Order).ThenBy(t => t.TableKey)
            .Select(t =>
            {
                var sideA = t.Docks
                    .Where(d => d.Side == DockSide.A)
                    .OrderBy(d => d.Code)
                    .Select(d => BuildSeatDto(d, blockedMap, reservationsByDock, incidenceCounts))
                    .ToList();

                var sideB = t.Docks
                    .Where(d => d.Side == DockSide.B)
                    .OrderBy(d => d.Code)
                    .Select(d => BuildSeatDto(d, blockedMap, reservationsByDock, incidenceCounts))
                    .ToList();

                var tableName = BuildTableName(zone.Name, t.TableKey);
                return new DockTableDto(tableName, t.Locator, sideA, sideB);
            })
            .ToList();

        return new DockZoneMapDto(zone.Id, zone.Name, zone.Description, zone.Orientation, tables);
    }

    internal static DockSeatDto BuildSeatDto(
        Entities.Dock dock,
        Dictionary<Guid, Entities.BlockedDock> blockedMap,
        Dictionary<Guid, List<Entities.Reservation>> reservationsByDock,
        Dictionary<Guid, Dictionary<IncidenceStatus, int>> incidenceCounts)
    {
        incidenceCounts.TryGetValue(dock.Id, out var counts);

        if (blockedMap.TryGetValue(dock.Id, out var block))
            return new DockSeatDto(
                dock.Id, dock.Code, DockStatus.Blocked, null,
                BlockedByName: block.BlockedByUser?.Name,
                BlockReason: block.Reason,
                IncidenceCounts: counts);

        if (!reservationsByDock.TryGetValue(dock.Id, out var dockReservations) || dockReservations.Count == 0)
            return new DockSeatDto(dock.Id, dock.Code, DockStatus.Free, null, IncidenceCounts: counts);

        // Determinar quién ocupa cada franja
        string? morningName = null;
        string? afternoonName = null;
        foreach (var r in dockReservations)
        {
            var name = r.User?.Name;
            if (r.TimeSlot is TimeSlot.Morning or TimeSlot.Both)
                morningName = name;
            if (r.TimeSlot is TimeSlot.Afternoon or TimeSlot.Both)
                afternoonName = name;
        }

        var totalSlots = dockReservations.Sum(r => SlotCount(r.TimeSlot));

        if (totalSlots >= 2)
            return new DockSeatDto(dock.Id, dock.Code, DockStatus.FullyBooked, null, morningName, afternoonName, IncidenceCounts: counts);

        // Parcialmente reservado: informar qué franja está ocupada
        var bookedSlot = dockReservations[0].TimeSlot;
        return new DockSeatDto(dock.Id, dock.Code, DockStatus.PartiallyBooked, bookedSlot, morningName, afternoonName, IncidenceCounts: counts);
    }

    private static string BuildTableName(string zoneName, string tableKey)
    {
        // "N" → nombre de la zona (mesa única), "D-1" → "Mesa 1", "D-2" → "Mesa 2"
        if (!tableKey.Contains('-'))
            return zoneName;

        var tableNumber = tableKey[(tableKey.IndexOf('-') + 1)..];
        return $"Mesa {tableNumber}";
    }

    private static int SlotCount(TimeSlot slot) => slot == TimeSlot.Both ? 2 : 1;
}
