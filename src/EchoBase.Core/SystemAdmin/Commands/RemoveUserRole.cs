using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Commands;

/// <summary>
/// Comando para retirar un rol a un usuario del sistema.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza la operación.</param>
/// <param name="TargetUserId">Identificador del usuario al que se retira el rol.</param>
/// <param name="RoleName">Nombre del rol a retirar (ej.: <c>Manager</c>, <c>SystemAdmin</c>).</param>
public sealed record RemoveUserRoleCommand(
    Guid AdminUserId,
    Guid TargetUserId,
    string RoleName) : IRequest<Result>, IAuditableRequest
{
    internal string? ResolvedTargetUserName { get; set; }
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.UserRoleRemoved;
    string IAuditableRequest.BuildAuditDetails() =>
        $"Rol '{RoleName}' retirado a: {ResolvedTargetUserName ?? TargetUserId.ToString()}";
}

/// <summary>
/// Handler del comando <see cref="RemoveUserRoleCommand"/>.
/// </summary>
public sealed class RemoveUserRoleHandler(
    IBlockedDockRepository blockedDockRepository,
    IUserRepository userRepository)
    : IRequestHandler<RemoveUserRoleCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";
    private static readonly IReadOnlySet<string> ValidRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Manager", "SystemAdmin" };

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveUserRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede retirar roles
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        // 2. El rol debe ser válido
        if (!ValidRoles.Contains(request.RoleName))
            return Result.Failure(SystemAdminErrors.InvalidRole);

        // 3. El usuario destino debe existir con sus roles cargados
        var user = await userRepository.GetWithRolesAsync(request.TargetUserId, cancellationToken);
        if (user is null)
            return Result.Failure(SystemAdminErrors.UserNotFound);

        // 4. El usuario debe tener el rol
        var roleToRemove = user.Roles.FirstOrDefault(
            r => r.Name.Equals(request.RoleName, StringComparison.OrdinalIgnoreCase));

        if (roleToRemove is null)
            return Result.Failure(SystemAdminErrors.RoleNotAssigned);

        user.Roles.Remove(roleToRemove);
        await userRepository.SaveChangesAsync(cancellationToken);
        request.ResolvedTargetUserName = user.Name;

        return Result.Success();
    }
}
