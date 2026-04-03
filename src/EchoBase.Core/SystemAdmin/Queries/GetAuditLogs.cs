using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Queries;

/// <summary>
/// Resultado paginado genérico usado en el log de auditoría.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    /// <summary>Número total de páginas disponibles.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

/// <summary>
/// Consulta paginada del log de auditoría con soporte para filtros.
/// </summary>
/// <param name="FromDate">Fecha de inicio del período a consultar (inclusiva). <see langword="null"/> para sin límite.</param>
/// <param name="ToDate">Fecha de fin del período a consultar (inclusiva). <see langword="null"/> para sin límite.</param>
/// <param name="ActionFilter">Filtro por tipo de acción. <see langword="null"/> para todas.</param>
/// <param name="UserSearch">Texto libre para buscar en el nombre del usuario. <see langword="null"/> para todos.</param>
/// <param name="Page">Número de página (base 1).</param>
/// <param name="PageSize">Registros por página.</param>
public sealed record GetAuditLogsQuery(
    DateOnly? FromDate,
    DateOnly? ToDate,
    AuditAction? ActionFilter,
    string? UserSearch,
    int Page,
    int PageSize) : IRequest<PagedResult<AuditLogDto>>;

/// <summary>
/// Handler de <see cref="GetAuditLogsQuery"/>.
/// </summary>
public sealed class GetAuditLogsHandler(IAuditLogRepository repository)
    : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    /// <inheritdoc />
    public async Task<PagedResult<AuditLogDto>> Handle(
        GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.GetPagedAsync(
            request.FromDate,
            request.ToDate,
            request.ActionFilter,
            request.UserSearch,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<AuditLogDto>(items, total, request.Page, request.PageSize);
    }
}
