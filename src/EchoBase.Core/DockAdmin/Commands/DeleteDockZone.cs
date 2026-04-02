using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para eliminar una zona de trabajo vacía (sin puestos asignados).
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la eliminación.</param>
/// <param name="ZoneId">Identificador de la zona a eliminar.</param>
public sealed record DeleteDockZoneCommand(
    Guid AdminUserId,
    Guid ZoneId) : IRequest<Result>, IAuditableRequest
{
    internal string? ResolvedZoneName { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockZoneDeleted;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Zona eliminada: '{ResolvedZoneName ?? ZoneId.ToString()}'";
}

/// <summary>
/// Handler del comando <see cref="DeleteDockZoneCommand"/>.
/// </summary>
public sealed class DeleteDockZoneHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<DeleteDockZoneCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteDockZoneCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede eliminar zonas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. La zona debe existir
        var zone = await dockAdminRepository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        if (zone is null)
            return Result.Failure(DockAdminErrors.ZoneNotFound);

        // 3. La zona no puede tener puestos asignados
        if (zone.Docks.Count > 0)
            return Result.Failure(DockAdminErrors.ZoneHasDocks);

        request.ResolvedZoneName = zone.Name;

        // 4. Eliminar (las mesas se eliminan en cascada por FK)
        await dockAdminRepository.DeleteZoneAsync(zone, cancellationToken);

        return Result.Success();
    }
}
