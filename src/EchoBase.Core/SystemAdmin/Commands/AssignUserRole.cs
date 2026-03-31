using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Commands;

/// <summary>
/// Comando para asignar un rol a un usuario del sistema.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la asignación.</param>
/// <param name="TargetUserId">Identificador del usuario al que se asigna el rol.</param>
/// <param name="RoleName">Nombre del rol a asignar (ej.: <c>Manager</c>, <c>SystemAdmin</c>).</param>
public sealed record AssignUserRoleCommand(
    Guid AdminUserId,
    Guid TargetUserId,
    string RoleName) : IRequest<Result>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.UserRoleAssigned;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Rol '{RoleName}' asignado al usuario {TargetUserId}";
}

/// <summary>
/// Handler del comando <see cref="AssignUserRoleCommand"/>.
/// </summary>
public sealed class AssignUserRoleHandler(
    IBlockedDockRepository blockedDockRepository,
    IUserRepository userRepository)
    : IRequestHandler<AssignUserRoleCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";
    private static readonly IReadOnlySet<string> ValidRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Manager", "SystemAdmin" };

    /// <inheritdoc />
    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede asignar roles
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El rol debe ser válido
        if (!ValidRoles.Contains(request.RoleName))
            return Result.Failure(SystemAdminErrors.InvalidRole);

        // 3. El usuario destino debe existir (con roles cargados)
        var user = await userRepository.GetWithRolesAsync(request.TargetUserId, cancellationToken);
        if (user is null)
            return Result.Failure(SystemAdminErrors.UserNotFound);

        // 4. El usuario no debe tener ya el rol
        if (user.Roles.Any(r => r.Name.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure(SystemAdminErrors.RoleAlreadyAssigned);

        // 5. Obtener la entidad Role y asignarla
        var role = await userRepository.GetRoleByNameAsync(request.RoleName, cancellationToken);
        if (role is null)
            return Result.Failure(SystemAdminErrors.InvalidRole);

        user.Roles.Add(role);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
