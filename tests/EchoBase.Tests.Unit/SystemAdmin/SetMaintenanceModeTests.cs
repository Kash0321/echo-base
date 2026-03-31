using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using NSubstitute;

namespace EchoBase.Tests.Unit.SystemAdmin;

public class SetMaintenanceModeTests
{
    private static readonly Guid AdminId = Guid.NewGuid();

    private readonly IBlockedDockRepository _blockedDockRepo = Substitute.For<IBlockedDockRepository>();
    private readonly ISystemSettingRepository _settingRepo = Substitute.For<ISystemSettingRepository>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly SetMaintenanceModeHandler _handler;

    public SetMaintenanceModeTests()
    {
        _time.GetUtcNow().Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        _handler = new(_blockedDockRepo, _settingRepo, _time);
    }

    private static SetMaintenanceModeCommand ActivateCmd(Guid? adminId = null, string? reason = null) =>
        new(adminId ?? AdminId, true, reason ?? "Actualización");

    private static SetMaintenanceModeCommand DeactivateCmd(Guid? adminId = null) =>
        new(adminId ?? AdminId, false, null);

    // ── Happy paths ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_ActivateBySystemAdmin_ReturnsSuccess()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ActivateCmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _settingRepo.Received(2).SetAsync(
            Arg.Any<string>(), Arg.Any<string>(), AdminId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _settingRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivateBySystemAdmin_ReturnsSuccess()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(DeactivateCmd(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ActivateStoresMaintenanceModeKey()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(ActivateCmd(reason: "Pruebas"), CancellationToken.None);

        await _settingRepo.Received(1).SetAsync(
            SystemSetting.MaintenanceModeKey, "true", AdminId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _settingRepo.Received(1).SetAsync(
            SystemSetting.MaintenanceModeReasonKey, "Pruebas", AdminId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeactivateClearsReason()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(DeactivateCmd(), CancellationToken.None);

        await _settingRepo.Received(1).SetAsync(
            SystemSetting.MaintenanceModeKey, "false", AdminId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await _settingRepo.Received(1).SetAsync(
            SystemSetting.MaintenanceModeReasonKey, string.Empty, AdminId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    // ── Authorization ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsNotSystemAdminError()
    {
        _blockedDockRepo.UserHasRoleAsync(AdminId, "SystemAdmin", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ActivateCmd(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
        await _settingRepo.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
