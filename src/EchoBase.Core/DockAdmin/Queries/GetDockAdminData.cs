using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.DockAdmin.Queries;

// ── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>DTO de un puesto de trabajo para la vista de administración.</summary>
public sealed record DockAdminDto(
    Guid Id,
    string Code,
    string Location,
    string Equipment,
    Guid DockTableId,
    DockSide Side);

/// <summary>DTO de una mesa física para la vista de administración. Incluye sus puestos de trabajo.</summary>
public sealed record DockTableAdminDto(
    Guid Id,
    string TableKey,
    string? Locator,
    Guid DockZoneId,
    int Order,
    IReadOnlyList<DockAdminDto> Docks);

/// <summary>DTO de una zona de trabajo completa para la vista de administración.</summary>
public sealed record DockZoneAdminDto(
    Guid Id,
    string Name,
    string? Description,
    ZoneOrientation Orientation,
    int Order,
    IReadOnlyList<DockTableAdminDto> Tables);

// ── Query y handler ───────────────────────────────────────────────────────────

/// <summary>
/// Consulta que devuelve la vista completa de zonas, mesas y puestos
/// para la pestaña de configuración del SystemAdmin.
/// </summary>
public sealed record GetDockAdminDataQuery : IRequest<IReadOnlyList<DockZoneAdminDto>>;

/// <summary>
/// Handler de <see cref="GetDockAdminDataQuery"/>.
/// </summary>
public sealed class GetDockAdminDataHandler(IDockAdminRepository dockAdminRepository)
    : IRequestHandler<GetDockAdminDataQuery, IReadOnlyList<DockZoneAdminDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DockZoneAdminDto>> Handle(
        GetDockAdminDataQuery request, CancellationToken cancellationToken)
    {
        var zones = await dockAdminRepository.GetAllZonesWithDetailsAsync(cancellationToken);

        return zones.Select(z => new DockZoneAdminDto(
            z.Id,
            z.Name,
            z.Description,
            z.Orientation,
            z.Order,
            z.Tables.OrderBy(t => t.Order).ThenBy(t => t.TableKey).Select(t => new DockTableAdminDto(
                t.Id,
                t.TableKey,
                t.Locator,
                t.DockZoneId,
                t.Order,
                t.Docks.Select(d => new DockAdminDto(d.Id, d.Code, d.Location, d.Equipment, d.DockTableId, d.Side)).ToList()
            )).ToList()
        )).ToList();
    }
}
