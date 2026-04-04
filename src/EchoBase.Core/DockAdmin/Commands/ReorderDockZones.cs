using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para reordenar las zonas de trabajo mediante drag-and-drop.
/// La lista <see cref="OrderedZoneIds"/> define el nuevo orden de visualización:
/// el primer elemento recibirá <c>Order = 0</c>, el segundo <c>Order = 1</c>, etc.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la reordenación.</param>
/// <param name="OrderedZoneIds">Lista de IDs de zonas en el nuevo orden deseado.</param>
public sealed record ReorderDockZonesCommand(
    Guid AdminUserId,
    IReadOnlyList<Guid> OrderedZoneIds) : IRequest<Result>, IAuditableRequest
{
    internal IReadOnlyList<string>? ResolvedZoneNames { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockZonesReordered;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Zonas reordenadas: {string.Join(", ", ResolvedZoneNames?.Select((name, i) => $"{name}→{i}") ?? OrderedZoneIds.Select((id, i) => $"{id}→{i}"))}";
}

/// <summary>
/// Handler del comando <see cref="ReorderDockZonesCommand"/>.
/// </summary>
public sealed class ReorderDockZonesHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<ReorderDockZonesCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(ReorderDockZonesCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede reordenar zonas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. Resolver nombres de zonas para auditoría
        var zones = await Task.WhenAll(request.OrderedZoneIds.Select(id => dockAdminRepository.GetZoneByIdAsync(id, cancellationToken)));
        request.ResolvedZoneNames = zones.Select(z => z?.Name ?? "Desconocida").ToList();

        // 3. Asignar Order = índice para cada zona
        var items = request.OrderedZoneIds
            .Select((id, idx) => (Id: id, Order: idx))
            .ToList();

        await dockAdminRepository.UpdateZoneOrdersAsync(items, cancellationToken);

        return Result.Success();
    }
}
