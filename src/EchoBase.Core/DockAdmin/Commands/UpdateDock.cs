using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para actualizar el código, ubicación y equipamiento de un puesto de trabajo existente.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la actualización.</param>
/// <param name="DockId">Identificador del puesto a actualizar.</param>
/// <param name="Code">Nuevo código del puesto.</param>
/// <param name="Side">Nuevo lado del puesto (A o B).</param>
/// <param name="Location">Nueva ubicación del puesto.</param>
/// <param name="Equipment">Nuevo equipamiento disponible.</param>
public sealed record UpdateDockCommand(
    Guid AdminUserId,
    Guid DockId,
    string Code,
    DockSide Side,
    string Location,
    string Equipment) : IRequest<Result>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockUpdated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Puesto actualizado: '{Code}' (Id: {DockId})";
}

/// <summary>
/// Handler del comando <see cref="UpdateDockCommand"/>.
/// </summary>
public sealed class UpdateDockHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<UpdateDockCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDockCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede actualizar puestos
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El código no puede estar vacío
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result.Failure(DockAdminErrors.DockCodeRequired);

        // 3. El puesto debe existir
        var dock = await dockAdminRepository.GetDockByIdAsync(request.DockId, cancellationToken);
        if (dock is null)
            return Result.Failure(DockAdminErrors.DockNotFound);

        // 4. El nuevo código debe ser único (excluyendo este puesto)
        if (await dockAdminRepository.DockCodeExistsAsync(request.Code, excludeId: request.DockId, cancellationToken))
            return Result.Failure(DockAdminErrors.DockCodeAlreadyExists);

        // 5. Aplicar la actualización
        await dockAdminRepository.UpdateDockAsync(
            request.DockId, request.Code.Trim(), request.Side, request.Location, request.Equipment, cancellationToken);

        return Result.Success();
    }
}
