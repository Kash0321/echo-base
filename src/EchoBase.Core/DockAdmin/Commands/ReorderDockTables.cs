using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para reordenar las mesas de una zona mediante drag-and-drop.
/// La lista <see cref="OrderedTableIds"/> define el nuevo orden dentro de la zona:
/// el primer elemento recibirá <c>Order = 0</c>, el segundo <c>Order = 1</c>, etc.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la reordenación.</param>
/// <param name="ZoneId">Identificador de la zona cuyas mesas se reordenan.</param>
/// <param name="OrderedTableIds">Lista de IDs de mesas en el nuevo orden deseado.</param>
public sealed record ReorderDockTablesCommand(
    Guid AdminUserId,
    Guid ZoneId,
    IReadOnlyList<Guid> OrderedTableIds) : IRequest<Result>, IAuditableRequest
{
    internal IReadOnlyList<string>? ResolvedTableNames { get; set; }
    internal string? ResolvedZoneName { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockTablesReordered;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Mesas reordenadas en zona {(ResolvedZoneName ?? ZoneId.ToString())}: {string.Join(", ", ResolvedTableNames?.Select((name, i) => $"{name}→{i}") ?? OrderedTableIds.Select((id, i) => $"{id}→{i}"))}";
}

/// <summary>
/// Handler del comando <see cref="ReorderDockTablesCommand"/>.
/// </summary>
public sealed class ReorderDockTablesHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<ReorderDockTablesCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderDockTablesCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede reordenar mesas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. Resolver nombres para auditoría
        var zone = await dockAdminRepository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        request.ResolvedZoneName = zone?.Name ?? "Desconocida";

        var tables = await Task.WhenAll(request.OrderedTableIds.Select(id => dockAdminRepository.GetTableByIdAsync(id, cancellationToken)));
        request.ResolvedTableNames = tables.Select(t => t?.TableKey ?? "Desconocida").ToList();

        // 3. Asignar Order = índice para cada mesa
        var items = request.OrderedTableIds
            .Select((id, idx) => (Id: id, Order: idx))
            .ToList();

        await dockAdminRepository.UpdateTableOrdersAsync(items, cancellationToken);

        return Result.Success();
    }
}
