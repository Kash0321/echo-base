using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Incidences.Queries;

/// <summary>
/// Consulta para obtener las incidencias reportadas por el usuario actual.
/// </summary>
/// <param name="UserId">Identificador del usuario.</param>
public sealed record GetUserIncidencesQuery(Guid UserId)
    : IRequest<IReadOnlyList<IncidenceReportDto>>;

/// <summary>
/// Handler que devuelve los reportes de incidencias del usuario, ordenados de más reciente a más antiguo.
/// </summary>
public sealed class GetUserIncidencesHandler(IIncidenceRepository incidenceRepository)
    : IRequestHandler<GetUserIncidencesQuery, IReadOnlyList<IncidenceReportDto>>
{
    public async Task<IReadOnlyList<IncidenceReportDto>> Handle(
        GetUserIncidencesQuery request, CancellationToken cancellationToken)
    {
        var reports = await incidenceRepository.GetUserIncidencesAsync(request.UserId, cancellationToken);

        return reports.Select(r => new IncidenceReportDto(
            r.Id,
            r.Dock?.Code ?? string.Empty,
            r.Dock?.Location ?? string.Empty,
            r.Description,
            r.Status,
            r.CreatedAt,
            r.UpdatedAt,
            r.ManagerComment)).ToList();
    }
}
