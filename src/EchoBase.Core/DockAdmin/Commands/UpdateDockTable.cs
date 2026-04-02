using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para actualizar el localizador (texto visible en el mapa) de una mesa lógica existente.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la actualización.</param>
/// <param name="TableId">Identificador de la mesa lógica a actualizar.</param>
/// <param name="Locator">Nuevo texto de localización (puede ser <see langword="null"/> para usar el nombre inferido).</param>
public sealed record UpdateDockTableCommand(
    Guid AdminUserId,
    Guid TableId,
    string? Locator) : IRequest<Result>, IAuditableRequest
{
    internal string? ResolvedTableKey { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockTableUpdated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Mesa actualizada: clave '{ResolvedTableKey ?? TableId.ToString()}'" +
        (Locator is not null ? $" — localizador: '{Locator}'" : " — localizador eliminado");
}

/// <summary>
/// Handler del comando <see cref="UpdateDockTableCommand"/>.
/// </summary>
public sealed class UpdateDockTableHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<UpdateDockTableCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDockTableCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede actualizar mesas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. La mesa debe existir
        var table = await dockAdminRepository.GetTableByIdAsync(request.TableId, cancellationToken);
        if (table is null)
            return Result.Failure(DockAdminErrors.TableNotFound);

        request.ResolvedTableKey = table.TableKey;

        // 3. Actualizar el localizador
        await dockAdminRepository.UpdateTableLocatorAsync(
            request.TableId, request.Locator?.Trim(), cancellationToken);

        return Result.Success();
    }
}
