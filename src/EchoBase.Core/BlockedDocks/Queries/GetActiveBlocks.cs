using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.BlockedDocks.Queries;

/// <summary>
/// Consulta que devuelve todos los bloqueos activos, incluyendo los que ya han pasado su fecha de fin.
/// Utilizada por el cuadro de mando del Manager para gestionar los bloqueos existentes.
/// </summary>
public sealed record GetActiveBlocksQuery : IRequest<IReadOnlyList<ActiveBlockDto>>;

/// <summary>
/// DTO que representa un bloqueo activo de puesto de trabajo.
/// </summary>
/// <param name="BlockId">Identificador único del bloqueo.</param>
/// <param name="DockId">Identificador del puesto bloqueado.</param>
/// <param name="DockCode">Código alfanumérico del puesto bloqueado (ej.: N-A01).</param>
/// <param name="StartDate">Fecha de inicio del bloqueo (inclusiva).</param>
/// <param name="EndDate">Fecha de fin del bloqueo (inclusiva).</param>
/// <param name="Reason">Motivo del bloqueo.</param>
/// <param name="BlockedByName">Nombre del Manager que creó el bloqueo.</param>
public sealed record ActiveBlockDto(
    Guid BlockId,
    Guid DockId,
    string DockCode,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string BlockedByName);

/// <summary>
/// Handler de <see cref="GetActiveBlocksQuery"/>.
/// Proyecta los bloqueos activos a <see cref="ActiveBlockDto"/> para el cuadro de mando.
/// </summary>
public sealed class GetActiveBlocksHandler(
    IBlockedDockRepository repository)
    : IRequestHandler<GetActiveBlocksQuery, IReadOnlyList<ActiveBlockDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ActiveBlockDto>> Handle(
        GetActiveBlocksQuery request, CancellationToken cancellationToken)
    {
        var blocks = await repository.GetAllActiveBlocksAsync(cancellationToken);

        return blocks
            .Select(b => new ActiveBlockDto(
                b.Id,
                b.DockId,
                b.Dock?.Code ?? b.DockId.ToString(),
                b.StartDate,
                b.EndDate,
                b.Reason,
                b.BlockedByUser?.Name ?? string.Empty))
            .ToList();
    }
}
