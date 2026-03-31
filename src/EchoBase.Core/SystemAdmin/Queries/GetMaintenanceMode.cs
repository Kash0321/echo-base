using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Queries;

/// <summary>
/// DTO que representa el estado actual del modo de mantenimiento.
/// </summary>
public sealed record MaintenanceModeDto(
    bool IsActive,
    string? Reason,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId);

/// <summary>
/// Consulta que devuelve el estado actual del modo de mantenimiento.
/// </summary>
public sealed record GetMaintenanceModeQuery : IRequest<MaintenanceModeDto>;

/// <summary>
/// Handler de <see cref="GetMaintenanceModeQuery"/>.
/// </summary>
public sealed class GetMaintenanceModeHandler(ISystemSettingRepository settingRepository)
    : IRequestHandler<GetMaintenanceModeQuery, MaintenanceModeDto>
{
    /// <inheritdoc />
    public async Task<MaintenanceModeDto> Handle(
        GetMaintenanceModeQuery request, CancellationToken cancellationToken)
    {
        var modeSetting = await settingRepository.GetSettingAsync(SystemSetting.MaintenanceModeKey, cancellationToken);
        var isActive = modeSetting?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        var reason = await settingRepository.GetValueAsync(SystemSetting.MaintenanceModeReasonKey, cancellationToken);

        return new MaintenanceModeDto(
            isActive,
            reason,
            modeSetting?.UpdatedAt,
            modeSetting?.UpdatedByUserId);
    }
}
