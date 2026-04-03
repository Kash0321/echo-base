using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para actualizar el nombre, descripción y orientación de una zona existente.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la actualización.</param>
/// <param name="ZoneId">Identificador de la zona a actualizar.</param>
/// <param name="Name">Nuevo nombre de la zona.</param>
/// <param name="Description">Nueva descripción de la zona (puede ser <see langword="null"/>).</param>
/// <param name="Orientation">Nueva orientación visual de las mesas.</param>
public sealed record UpdateDockZoneCommand(
    Guid AdminUserId,
    Guid ZoneId,
    string Name,
    string? Description,
    ZoneOrientation Orientation) : IRequest<Result>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockZoneUpdated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Zona actualizada: '{Name}' (Id: {ZoneId}, orientación: {Orientation})";
}

/// <summary>
/// Handler del comando <see cref="UpdateDockZoneCommand"/>.
/// </summary>
public sealed class UpdateDockZoneHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<UpdateDockZoneCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDockZoneCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede actualizar zonas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. La zona debe existir
        var zone = await dockAdminRepository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        if (zone is null)
            return Result.Failure(DockAdminErrors.ZoneNotFound);

        // 3. El nombre debe ser único (excluyendo esta zona)
        if (await dockAdminRepository.ZoneNameExistsAsync(request.Name, excludeId: request.ZoneId, cancellationToken))
            return Result.Failure(DockAdminErrors.ZoneNameAlreadyExists);

        // 4. Aplicar la actualización
        await dockAdminRepository.UpdateZoneAsync(
            request.ZoneId, request.Name, request.Description, request.Orientation, cancellationToken);

        return Result.Success();
    }
}
