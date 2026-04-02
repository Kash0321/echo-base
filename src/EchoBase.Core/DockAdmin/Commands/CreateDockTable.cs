using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para crear una nueva mesa lógica dentro de una zona.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la creación.</param>
/// <param name="ZoneId">Identificador de la zona a la que pertenece la mesa.</param>
/// <param name="TableKey">Clave lógica única de la mesa dentro de la zona (ej.: <c>"N"</c>, <c>"D-1"</c>).</param>
/// <param name="Locator">Texto descriptivo opcional visible en el mapa (ej.: <c>"Mesa central"</c>).</param>
public sealed record CreateDockTableCommand(
    Guid AdminUserId,
    Guid ZoneId,
    string TableKey,
    string? Locator) : IRequest<Result<Guid>>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockTableCreated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Mesa creada: clave '{TableKey}' en zona {ZoneId}" +
        (Locator is not null ? $" — localizador: '{Locator}'" : string.Empty);
}

/// <summary>
/// Handler del comando <see cref="CreateDockTableCommand"/>.
/// </summary>
public sealed class CreateDockTableHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<CreateDockTableCommand, Result<Guid>>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateDockTableCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede crear mesas
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<Guid>.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. La clave no puede estar vacía
        if (string.IsNullOrWhiteSpace(request.TableKey))
            return Result<Guid>.Failure(DockAdminErrors.TableKeyRequired);

        // 3. La zona debe existir
        var zone = await dockAdminRepository.GetZoneByIdAsync(request.ZoneId, cancellationToken);
        if (zone is null)
            return Result<Guid>.Failure(DockAdminErrors.ZoneNotFound);

        // 4. La clave debe ser única dentro de la zona
        if (await dockAdminRepository.TableKeyExistsInZoneAsync(request.TableKey, request.ZoneId, excludeId: null, cancellationToken))
            return Result<Guid>.Failure(DockAdminErrors.TableKeyAlreadyExists);

        // 5. Crear la mesa
        var tableId = Guid.CreateVersion7();
        var table = new DockTable(tableId)
        {
            TableKey = request.TableKey.Trim(),
            Locator  = request.Locator?.Trim()
        };
        table.AssignToZone(zone);

        await dockAdminRepository.AddTableAsync(table, cancellationToken);
        await dockAdminRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tableId);
    }
}
