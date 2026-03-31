using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Commands;

/// <summary>
/// Comando para activar o desactivar el modo de mantenimiento del sistema.
/// Solo los usuarios con rol <c>SystemAdmin</c> pueden ejecutar esta acción.
/// </summary>
/// <param name="AdminUserId">Identificador del SystemAdmin que realiza el cambio.</param>
/// <param name="IsActive">
/// <see langword="true"/> para activar el modo de mantenimiento;
/// <see langword="false"/> para desactivarlo.
/// </param>
/// <param name="Reason">Motivo del mantenimiento (requerido al activar).</param>
public sealed record SetMaintenanceModeCommand(
    Guid AdminUserId,
    bool IsActive,
    string? Reason) : IRequest<Result>, IAuditableRequest
{
    Guid? IAuditableRequest.PerformedByUserId => AdminUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.MaintenanceModeChanged;
    string IAuditableRequest.BuildAuditDetails() =>
        IsActive
            ? $"Modo mantenimiento ACTIVADO. Motivo: {Reason}"
            : "Modo mantenimiento DESACTIVADO";
}

/// <summary>
/// Handler del comando <see cref="SetMaintenanceModeCommand"/>.
/// </summary>
public sealed class SetMaintenanceModeHandler(
    IBlockedDockRepository blockedDockRepository,
    ISystemSettingRepository settingRepository,
    TimeProvider timeProvider)
    : IRequestHandler<SetMaintenanceModeCommand, Result>
{
    private const string SystemAdminRole = "SystemAdmin";

    /// <inheritdoc />
    public async Task<Result> Handle(SetMaintenanceModeCommand request, CancellationToken cancellationToken)
    {
        // 1. Solo SystemAdmin puede ejecutar esta acción
        if (!await blockedDockRepository.UserHasRoleAsync(request.AdminUserId, SystemAdminRole, cancellationToken))
            return Result.Failure(SystemAdminErrors.NotSystemAdmin);

        var now = timeProvider.GetUtcNow();

        // 2. Persistir el estado del modo mantenimiento
        await settingRepository.SetAsync(
            SystemSetting.MaintenanceModeKey,
            request.IsActive ? "true" : "false",
            request.AdminUserId,
            now,
            cancellationToken);

        // 3. Persistir el motivo (vacío si se desactiva)
        await settingRepository.SetAsync(
            SystemSetting.MaintenanceModeReasonKey,
            request.Reason ?? string.Empty,
            request.AdminUserId,
            now,
            cancellationToken);

        await settingRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
