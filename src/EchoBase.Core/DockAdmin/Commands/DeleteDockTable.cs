using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para eliminar una mesa lógica de una zona.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la eliminación.</param>
/// <param name="TableId">Identificador de la mesa lógica a eliminar.</param>
public sealed record DeleteDockTableCommand(
    Guid AdminUserId,
    Guid TableId) : IRequest<Result>, IAuditableRequest
{
    internal string? ResolvedTableKey { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockTableDeleted;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Mesa eliminada: clave '{ResolvedTableKey ?? TableId.ToString()}'";
}

/// <summary>
/// Handler del comando <see cref="DeleteDockTableCommand"/>.
/// </summary>
public sealed class DeleteDockTableHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<DeleteDockTableCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDockTableCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede eliminar mesas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. La mesa debe existir
        var table = await dockAdminRepository.GetTableByIdAsync(request.TableId, cancellationToken);
        if (table is null)
            return Result.Failure(DockAdminErrors.TableNotFound);

        request.ResolvedTableKey = table.TableKey;

        // 3. Eliminar la mesa
        await dockAdminRepository.DeleteTableAsync(table, cancellationToken);

        return Result.Success();
    }
}
