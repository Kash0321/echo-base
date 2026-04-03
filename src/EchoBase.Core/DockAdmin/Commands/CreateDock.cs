using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using MediatR;

namespace EchoBase.Core.DockAdmin.Commands;

/// <summary>
/// Comando para crear un nuevo puesto de trabajo y asignarlo a una mesa.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la creación.</param>
/// <param name="TableId">Identificador de la mesa física a la que se asignará el puesto.</param>
/// <param name="Side">Lado de la mesa al que pertenece el puesto (A o B).</param>
/// <param name="Code">Código alfanumérico único del puesto (ej.: <c>N-A07</c>).</param>
/// <param name="Location">Descripción de la ubicación física del puesto.</param>
/// <param name="Equipment">Equipamiento disponible en el puesto (texto libre).</param>
public sealed record CreateDockCommand(
    Guid AdminUserId,
    Guid TableId,
    DockSide Side,
    string Code,
    string Location,
    string Equipment) : IRequest<Result<Guid>>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.DockCreated;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Puesto creado: '{Code}' en mesa {TableId} (lado {Side})";
}

/// <summary>
/// Handler del comando <see cref="CreateDockCommand"/>.
/// </summary>
public sealed class CreateDockHandler(
    IBlockedDockRepository blockedDockRepository,
    IDockAdminRepository dockAdminRepository)
    : IRequestHandler<CreateDockCommand, Result<Guid>>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateDockCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede crear puestos
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result<Guid>.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El código no puede estar vacío
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<Guid>.Failure(DockAdminErrors.DockCodeRequired);

        // 3. El código debe ser único
        if (await dockAdminRepository.DockCodeExistsAsync(request.Code, excludeId: null, cancellationToken))
            return Result<Guid>.Failure(DockAdminErrors.DockCodeAlreadyExists);

        // 4. La mesa debe existir
        var table = await dockAdminRepository.GetTableByIdAsync(request.TableId, cancellationToken);
        if (table is null)
            return Result<Guid>.Failure(DockAdminErrors.TableNotFound);

        // 5. Crear el puesto
        var dockId = Guid.CreateVersion7();
        var dock = new Dock(dockId)
        {
            Code      = request.Code.Trim(),
            Location  = request.Location,
            Equipment = request.Equipment
        };
        dock.AssignToTable(table, request.Side);

        await dockAdminRepository.AddDockAsync(dock, cancellationToken);
        await dockAdminRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(dockId);
    }
}
