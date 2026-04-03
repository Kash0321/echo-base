using EchoBase.Core.Entities;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using EchoBase.Core.SystemAdmin.Queries;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.SystemAdmin;

/// <summary>
/// Tests de integración para <see cref="SetMaintenanceModeCommand"/>.
/// </summary>
public sealed class SetMaintenanceModeIntegrationTests : IntegrationTestBase
{
    // ── IT-SM-01 ──────────────────────────────────────────────────
    [Fact]
    public async Task SetMaintenanceMode_Activate_PersistsToDatabase()
    {
        // Arrange
        var command = new SetMaintenanceModeCommand(AdminUserId, true, "Actualización de sistema");

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var modeSetting = await DbContext.SystemSettings
            .SingleOrDefaultAsync(s => s.Key == SystemSetting.MaintenanceModeKey);
        Assert.NotNull(modeSetting);
        Assert.Equal("true", modeSetting.Value);

        var reasonSetting = await DbContext.SystemSettings
            .SingleOrDefaultAsync(s => s.Key == SystemSetting.MaintenanceModeReasonKey);
        Assert.NotNull(reasonSetting);
        Assert.Equal("Actualización de sistema", reasonSetting.Value);
    }

    // ── IT-SM-02 ──────────────────────────────────────────────────
    [Fact]
    public async Task SetMaintenanceMode_Deactivate_ClearsReason()
    {
        // Arrange: first activate
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Motivo temporal"));
        // Then deactivate
        var result = await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, false, null));

        // Assert
        Assert.True(result.IsSuccess);

        var modeSetting = await DbContext.SystemSettings
            .SingleOrDefaultAsync(s => s.Key == SystemSetting.MaintenanceModeKey);
        Assert.Equal("false", modeSetting!.Value);

        var reasonSetting = await DbContext.SystemSettings
            .SingleOrDefaultAsync(s => s.Key == SystemSetting.MaintenanceModeReasonKey);
        Assert.Equal(string.Empty, reasonSetting!.Value);
    }

    // ── IT-SM-03 ──────────────────────────────────────────────────
    [Fact]
    public async Task SetMaintenanceMode_ActivateThenQuery_ReturnsCorrectDto()
    {
        const string reason = "Mantenimiento programado";
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, reason));

        var dto = await Mediator.Send(new GetMaintenanceModeQuery());

        Assert.True(dto.IsActive);
        Assert.Equal(reason, dto.Reason);
        Assert.NotNull(dto.UpdatedAt);
        Assert.Equal(AdminUserId, dto.UpdatedByUserId);
    }

    // ── IT-SM-04 ──────────────────────────────────────────────────
    [Fact]
    public async Task SetMaintenanceMode_NonSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new SetMaintenanceModeCommand(TestUserId, true, "Test"));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ── IT-SM-05: AuditLog ────────────────────────────────────────
    [Fact]
    public async Task SetMaintenanceMode_Activate_WritesAuditLog()
    {
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Pronto mantenimiento"));

        var log = await DbContext.AuditLogs.SingleOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(EchoBase.Core.Entities.Enums.AuditAction.MaintenanceModeChanged, log.Action);
        Assert.Equal(AdminUserId, log.PerformedByUserId);
    }
}
