using EchoBase.Core.Common;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.BlockedDocks.Commands;

/// <summary>
/// Comando para desbloquear (desactivar) uno o varios bloqueos existentes.
/// Solo los usuarios con rol <c>Manager</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="ManagerUserId">Identificador del Manager que solicita el desbloqueo.</param>
/// <param name="BlockedDockIds">Identificadores de los registros de bloqueo a desactivar.</param>
public sealed record UnblockDocksCommand(
    Guid ManagerUserId,
    IReadOnlyList<Guid> BlockedDockIds) : IRequest<Result>;

/// <summary>
/// Handler que implementa las reglas de negocio para el desbloqueo de puestos.
/// </summary>
/// <remarks>
/// Reglas validadas:
/// <list type="number">
///   <item>El solicitante debe tener el rol <c>Manager</c>.</item>
///   <item>La lista de bloqueos no puede estar vacía.</item>
///   <item>Todos los bloqueos indicados deben existir.</item>
///   <item>Todos los bloqueos deben estar activos (no desactivados previamente).</item>
/// </list>
/// </remarks>
public sealed class UnblockDocksHandler(
    IBlockedDockRepository repository) : IRequestHandler<UnblockDocksCommand, Result>
{
    private const string ManagerRole = "Manager";

    /// <inheritdoc />
    public async Task<Result> Handle(UnblockDocksCommand request, CancellationToken cancellationToken)
    {
        // 1. El solicitante debe tener el rol Manager
        if (!await repository.UserHasRoleAsync(request.ManagerUserId, ManagerRole, cancellationToken))
            return Result.Failure(BlockedDockErrors.NotManager);

        // 2. La lista no puede estar vacía
        if (request.BlockedDockIds is not { Count: > 0 })
            return Result.Failure(BlockedDockErrors.EmptyDockList);

        // 3. Todos los bloqueos deben existir
        var blocks = await repository.GetByIdsAsync(request.BlockedDockIds, cancellationToken);

        if (blocks.Count != request.BlockedDockIds.Count)
            return Result.Failure(BlockedDockErrors.BlocksNotFound);

        // 4. Todos los bloqueos deben estar activos
        if (blocks.Exists(b => !b.IsActive))
            return Result.Failure(BlockedDockErrors.AlreadyDeactivated);

        // 5. Desactivar los bloqueos
        foreach (var block in blocks)
            block.Deactivate();

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
