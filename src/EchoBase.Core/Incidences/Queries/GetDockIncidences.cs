using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin.Queries;
using MediatR;

namespace EchoBase.Core.Incidences.Queries;

/// <summary>
/// Consulta para obtener las incidencias asociadas a un puesto de trabajo concreto.
/// Utilizada desde el modal de reporte para que el usuario pueda ver el historial
/// antes de abrir un nuevo reporte.
/// </summary>
/// <param name="DockId">Identificador del puesto de trabajo.</param>
/// <param name="ActiveOnly">
/// Cuando es <see langword="true"/> devuelve solo incidencias activas
/// (<see cref="IncidenceStatus.Open"/> y <see cref="IncidenceStatus.UnderReview"/>).
/// Cuando es <see langword="false"/> devuelve el historial completo.
/// </param>
/// <param name="Page">Número de página (base 1).</param>
/// <param name="PageSize">Número de elementos por página.</param>
public sealed record GetDockIncidencesQuery(
    Guid DockId,
    bool ActiveOnly,
    int Page = 1,
    int PageSize = 5) : IRequest<PagedResult<IncidenceReportDto>>;

/// <summary>
/// Handler que devuelve las incidencias de un puesto de trabajo, paginadas y filtradas.
/// </summary>
public sealed class GetDockIncidencesHandler(IIncidenceRepository incidenceRepository)
    : IRequestHandler<GetDockIncidencesQuery, PagedResult<IncidenceReportDto>>
{
    private static readonly IncidenceStatus[] ActiveStatuses =
        [IncidenceStatus.Open, IncidenceStatus.UnderReview];

    public async Task<PagedResult<IncidenceReportDto>> Handle(
        GetDockIncidencesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await incidenceRepository.GetDockIncidencesAsync(
            request.DockId,
            request.ActiveOnly ? ActiveStatuses : null,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(r => new IncidenceReportDto(
            r.Id,
            r.Dock?.Code ?? string.Empty,
            r.Dock?.Location ?? string.Empty,
            r.Description,
            r.Status,
            r.CreatedAt,
            r.UpdatedAt,
            r.ManagerComment)).ToList();

        return new PagedResult<IncidenceReportDto>(dtos, total, request.Page, request.PageSize);
    }
}
