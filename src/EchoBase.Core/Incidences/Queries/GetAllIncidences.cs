using EchoBase.Core.Common;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin.Queries;
using MediatR;

namespace EchoBase.Core.Incidences.Queries;

/// <summary>
/// Consulta para obtener todos los reportes de incidencias del sistema (vista de Manager).
/// Solo los usuarios con rol <c>Manager</c> pueden ejecutar esta consulta.
/// </summary>
/// <param name="ManagerUserId">Identificador del Manager que realiza la consulta.</param>
/// <param name="Page">Número de página (base 1).</param>
/// <param name="PageSize">Número de elementos por página.</param>
public sealed record GetAllIncidencesQuery(
    Guid ManagerUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<IncidenceReportManagerDto>>>;

/// <summary>
/// Handler que devuelve todos los reportes de incidencias paginados, validando que el usuario sea Manager.
/// </summary>
public sealed class GetAllIncidencesHandler(
    IBlockedDockRepository blockedDockRepository,
    IIncidenceRepository incidenceRepository)
    : IRequestHandler<GetAllIncidencesQuery, Result<PagedResult<IncidenceReportManagerDto>>>
{
    private const string ManagerRole = "Manager";

    public async Task<Result<PagedResult<IncidenceReportManagerDto>>> Handle(
        GetAllIncidencesQuery request, CancellationToken cancellationToken)
    {
        if (!await blockedDockRepository.UserHasRoleAsync(request.ManagerUserId, ManagerRole, cancellationToken))
            return Result<PagedResult<IncidenceReportManagerDto>>.Failure(IncidenceErrors.NotManager);

        var (items, total) = await incidenceRepository.GetAllIncidencesAsync(
            request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(r => new IncidenceReportManagerDto(
            r.Id,
            r.Dock?.Code ?? string.Empty,
            r.Dock?.Location ?? string.Empty,
            r.Description,
            r.Status,
            r.CreatedAt,
            r.UpdatedAt,
            r.ManagerComment,
            r.ReportedByUser?.Name ?? string.Empty,
            r.ReportedByUser?.Email ?? string.Empty)).ToList();

        return Result<PagedResult<IncidenceReportManagerDto>>.Success(
            new PagedResult<IncidenceReportManagerDto>(dtos, total, request.Page, request.PageSize));
    }
}
