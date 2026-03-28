using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.BlockedDocks.Commands;

/// <summary>
/// Comando para bloquear uno o varios puestos de trabajo durante un período de fechas.
/// Solo los usuarios con rol <c>Manager</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="ManagerUserId">Identificador del Manager que solicita el bloqueo.</param>
/// <param name="DockIds">Identificadores de los puestos a bloquear.</param>
/// <param name="StartDate">Fecha de inicio del bloqueo (inclusiva).</param>
/// <param name="EndDate">Fecha de fin del bloqueo (inclusiva).</param>
/// <param name="Reason">Motivo del bloqueo.</param>
public sealed record BlockDocksCommand(
    Guid ManagerUserId,
    IReadOnlyList<Guid> DockIds,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason) : IRequest<Result<IReadOnlyList<Guid>>>;

/// <summary>
/// Handler que implementa las reglas de negocio para el bloqueo de puestos de trabajo.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>El solicitante debe tener el rol <c>Manager</c>.</item>
///   <item>La lista de puestos no puede estar vacía.</item>
///   <item>La fecha de inicio no puede ser anterior a hoy.</item>
///   <item>La fecha de fin no puede ser anterior a la fecha de inicio.</item>
///   <item>El motivo no puede estar vacío.</item>
///   <item>Todos los puestos deben existir.</item>
///   <item>No puede haber bloqueos activos solapados para los mismos puestos y fechas.</item>
/// </list>
/// </remarks>
public sealed class BlockDocksHandler(
    IBlockedDockRepository repository,
    TimeProvider timeProvider) : IRequestHandler<BlockDocksCommand, Result<IReadOnlyList<Guid>>>
{
    private const string ManagerRole = "Manager";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Guid>>> Handle(
        BlockDocksCommand request, CancellationToken cancellationToken)
    {
        // 1. El solicitante debe tener el rol Manager
        if (!await repository.UserHasRoleAsync(request.ManagerUserId, ManagerRole, cancellationToken))
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.NotManager);

        // 2. La lista de puestos no puede estar vacía
        if (request.DockIds is not { Count: > 0 })
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.EmptyDockList);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // 3. La fecha de inicio no puede ser anterior a hoy
        if (request.StartDate < today)
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.StartDateInThePast);

        // 4. La fecha de fin no puede ser anterior a la de inicio
        if (request.EndDate < request.StartDate)
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.EndDateBeforeStartDate);

        // 5. El motivo no puede estar vacío
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.EmptyReason);

        // 6. Todos los puestos deben existir
        if (!await repository.AllDocksExistAsync(request.DockIds, cancellationToken))
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.DocksNotFound);

        // 7. No puede haber bloqueos activos solapados
        var overlapping = await repository.GetActiveBlocksForDocksAsync(
            request.DockIds, request.StartDate, request.EndDate, cancellationToken);

        if (overlapping.Count > 0)
            return Result<IReadOnlyList<Guid>>.Failure(BlockedDockErrors.OverlappingBlocks);

        // 8. Crear los bloqueos
        var blocks = request.DockIds.Select(dockId =>
            new BlockedDock(
                Guid.NewGuid(),
                dockId,
                request.ManagerUserId,
                request.StartDate,
                request.EndDate,
                request.Reason)).ToList();

        await repository.AddRangeAsync(blocks, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Guid> ids = blocks.Select(b => b.Id).ToList();
        return Result<IReadOnlyList<Guid>>.Success(ids);
    }
}
