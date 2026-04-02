using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para crear una nueva zona de trabajo.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la creación.</param>
/// <param name="Name">Nombre único de la nueva zona.</param>
/// <param name="Description">Descripción opcional de la zona.</param>
/// <param name="Orientation">Orientación visual de las mesas dentro de la zona.</param>
public sealed record CreateDockZoneCommand(
    Guid AdminUserId,
    string Name,
    string? Description,
    ZoneOrientation Orientation) : IRequest<Result<Guid>>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockZoneCreated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Zona creada: '{Name}' (orientación: {Orientation})";
}

/// <summary>
/// Handler del comando <see cref="CreateDockZoneCommand"/>.
/// </summary>
public sealed class CreateDockZoneHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<CreateDockZoneCommand, Result<Guid>>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateDockZoneCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede crear zonas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<Guid>.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El nombre de la zona debe ser único
        if (await dockAdminRepository.ZoneNameExistsAsync(request.Name, excludeId: null, cancellationToken))
            return Result<Guid>.Failure(DockAdminErrors.ZoneNameAlreadyExists);

        // 3. Crear la zona
        var zoneId = Guid.CreateVersion7();
        var zone = new DockZone(zoneId)
        {
            Name         = request.Name,
            Description  = request.Description,
            Orientation  = request.Orientation
        };

        await dockAdminRepository.AddZoneAsync(zone, cancellationToken);
        await dockAdminRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(zoneId);
    }
}
