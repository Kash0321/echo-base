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
public sealed record DockSeatDto(
    Guid Id,
    string Code,
    DockStatus Status,
    TimeSlot? BookedSlot,
    string? MorningBookedBy = null,
    string? AfternoonBookedBy = null,
    string? BlockedByName = null,
    string? BlockReason = null);

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
/// La estructura de mesas se infiere del código del puesto:
/// <list type="bullet">
///   <item><c>N-A</c> / <c>N-B</c> → Nostromo, mesa única, lados A y B</item>
///   <item><c>D-1A</c> / <c>D-1B</c> → Derelict, mesa 1, lados A y B</item>
///   <item><c>D-2A</c> / <c>D-2B</c> → Derelict, mesa 2, lados A y B</item>
/// </list>
/// </remarks>
internal sealed class GetDockMapHandler(IDockMapRepository repository)
    : IRequestHandler<GetDockMapQuery, DockMapDto>
{
    public async Task<DockMapDto> Handle(GetDockMapQuery request, CancellationToken cancellationToken)
    {
        var zones = await repository.GetAllZonesWithDocksAsync(cancellationToken);
        var reservations = await repository.GetAllActiveReservationsForDateAsync(request.Date, cancellationToken);
        var blockedDocks = await repository.GetBlockedDocksForDateAsync(request.Date, cancellationToken);

        var blockedMap = blockedDocks.ToDictionary(b => b.DockId);
        var reservationsByDock = reservations
            .GroupBy(r => r.DockId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var zoneDtos = zones
            .OrderByDescending(z => z.Name)
            .Select(z => BuildZoneDto(z, blockedMap, reservationsByDock))
            .ToList();

        return new DockMapDto(request.Date, zoneDtos);
    }

    internal static DockZoneMapDto BuildZoneDto(
        Entities.DockZone zone,
        Dictionary<Guid, Entities.BlockedDock> blockedMap,
        Dictionary<Guid, List<Entities.Reservation>> reservationsByDock)
    {
        // Agrupar puestos por mesa y lado según el patrón de código
        var docksByTable = zone.Docks
            .GroupBy(d => ParseTableKey(d.Code))
            .OrderBy(g => g.Key)
            .ToList();

        var tables = new List<DockTableDto>();

        // Construir lookup de localizadores por clave de mesa
        var locatorByKey = zone.Tables
            .ToDictionary(t => t.TableKey, t => t.Locator);

        foreach (var tableGroup in docksByTable)
        {
            var bySide = tableGroup
                .GroupBy(d => ParseSide(d.Code))
                .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Code).ToList());

            var sideA = bySide.GetValueOrDefault("A", [])
                .Select(d => BuildSeatDto(d, blockedMap, reservationsByDock))
                .ToList();

            var sideB = bySide.GetValueOrDefault("B", [])
                .Select(d => BuildSeatDto(d, blockedMap, reservationsByDock))
                .ToList();

            var tableName = BuildTableName(zone.Name, tableGroup.Key);
            var locator   = locatorByKey.GetValueOrDefault(tableGroup.Key);
            tables.Add(new DockTableDto(tableName, locator, sideA, sideB));
        }

        return new DockZoneMapDto(zone.Id, zone.Name, zone.Description, zone.Orientation, tables);
    }

    internal static DockSeatDto BuildSeatDto(
        Entities.Dock dock,
        Dictionary<Guid, Entities.BlockedDock> blockedMap,
        Dictionary<Guid, List<Entities.Reservation>> reservationsByDock)
    {
        if (blockedMap.TryGetValue(dock.Id, out var block))
            return new DockSeatDto(
                dock.Id, dock.Code, DockStatus.Blocked, null,
                BlockedByName: block.BlockedByUser?.Name,
                BlockReason: block.Reason);

        if (!reservationsByDock.TryGetValue(dock.Id, out var dockReservations) || dockReservations.Count == 0)
            return new DockSeatDto(dock.Id, dock.Code, DockStatus.Free, null);

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
            return new DockSeatDto(dock.Id, dock.Code, DockStatus.FullyBooked, null, morningName, afternoonName);

        // Parcialmente reservado: informar qué franja está ocupada
        var bookedSlot = dockReservations[0].TimeSlot;
        return new DockSeatDto(dock.Id, dock.Code, DockStatus.PartiallyBooked, bookedSlot, morningName, afternoonName);
    }

    /// <summary>
    /// Extrae la clave de mesa del código del puesto.
    /// <c>N-A01</c> → <c>N</c>, <c>D-1A01</c> → <c>D-1</c>, <c>D-2B03</c> → <c>D-2</c>.
    /// </summary>
    internal static string ParseTableKey(string code)
    {
        // Códigos: N-A01, N-B01, D-1A01, D-1B01, D-2A01, D-2B01
        var dashIndex = code.IndexOf('-');
        if (dashIndex < 0) return code;

        var afterDash = code[(dashIndex + 1)..];

        // Si empieza con dígito (D-1A01, D-2B03) → prefijo + dígito
        if (afterDash.Length > 0 && char.IsDigit(afterDash[0]))
            return $"{code[..dashIndex]}-{afterDash[0]}";

        // Si empieza con letra (N-A01, N-B01) → solo prefijo
        return code[..dashIndex];
    }

    /// <summary>
    /// Extrae el lado (A/B) del código del puesto.
    /// <c>N-A01</c> → <c>A</c>, <c>D-1B03</c> → <c>B</c>.
    /// </summary>
    internal static string ParseSide(string code)
    {
        var dashIndex = code.IndexOf('-');
        if (dashIndex < 0) return "A";

        var afterDash = code[(dashIndex + 1)..];

        // D-1A01 → afterDash = "1A01" → side en posición 1
        if (afterDash.Length > 1 && char.IsDigit(afterDash[0]))
            return afterDash[1].ToString();

        // N-A01 → afterDash = "A01" → side en posición 0
        return afterDash[0].ToString();
    }

    private static string BuildTableName(string zoneName, string tableKey)
    {
        // N → "Nostromo" (una sola mesa)
        if (!tableKey.Contains('-'))
            return zoneName;

        // D-1 → "Mesa 1", D-2 → "Mesa 2"
        var tableNumber = tableKey[(tableKey.IndexOf('-') + 1)..];
        return $"Mesa {tableNumber}";
    }

    private static int SlotCount(TimeSlot slot) => slot == TimeSlot.Both ? 2 : 1;
}
