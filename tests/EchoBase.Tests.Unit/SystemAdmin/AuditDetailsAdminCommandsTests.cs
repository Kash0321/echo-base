using EchoBase.Core.BlockedDocks.Commands;
using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.SystemAdmin.Commands;

namespace EchoBase.Tests.Unit.SystemAdmin;

/// <summary>
/// Verifica que <c>BuildAuditDetails()</c> de los comandos administrativos
/// produce texto legible con nombres reales en lugar de GUIDs.
/// </summary>
public class AuditDetailsAdminCommandsTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid TargetUserId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 4, 1);

    // UT-AD-05
    [Fact]
    public void BlockDocks_BuildAuditDetails_ContainsDockCodesAfterResolution()
    {
        var cmd = new BlockDocksCommand(
            AdminId,
            [DockId],
            Date,
            Date.AddDays(2),
            "Mantenimiento")
        {
            ResolvedDockCodes = ["N-A01"]
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("N-A01", details);
        Assert.Contains("01/04/2026", details);
        Assert.Contains("Mantenimiento", details);
        Assert.DoesNotContain(DockId.ToString(), details);
    }

    // UT-AD-06
    [Fact]
    public void BlockDocks_BuildAuditDetails_WithoutResolution_FallsBackToCount()
    {
        var cmd = new BlockDocksCommand(AdminId, [DockId, Guid.NewGuid()], Date, Date, "Motivo");

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("2 puesto(s)", details);
    }

    // UT-AD-07
    [Theory]
    [InlineData(TimeSlot.Morning,   "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both,      "Mañana y Tarde")]
    public void EmergencyReservation_BuildAuditDetails_ContainsUserNameAndDockCode(
        TimeSlot slot, string expectedSlotText)
    {
        var cmd = new CreateEmergencyReservationCommand(AdminId, TargetUserId, DockId, Date, slot)
        {
            ResolvedDockCode = "N-B03",
            ResolvedTargetUserName = "Ana García"
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("N-B03", details);
        Assert.Contains("Ana García", details);
        Assert.Contains("01/04/2026", details);
        Assert.Contains(expectedSlotText, details);
        Assert.DoesNotContain(DockId.ToString(), details);
        Assert.DoesNotContain(TargetUserId.ToString(), details);
    }

    // UT-AD-08
    [Fact]
    public void EmergencyReservation_BuildAuditDetails_WithoutResolution_FallsBackToGuids()
    {
        var cmd = new CreateEmergencyReservationCommand(AdminId, TargetUserId, DockId, Date, TimeSlot.Morning);

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains(DockId.ToString(), details);
        Assert.Contains(TargetUserId.ToString(), details);
    }

    // UT-AD-09
    [Fact]
    public void AssignRole_BuildAuditDetails_ContainsUserNameAfterResolution()
    {
        var cmd = new AssignUserRoleCommand(AdminId, TargetUserId, "Manager")
        {
            ResolvedTargetUserName = "Carlos López"
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("Manager", details);
        Assert.Contains("Carlos López", details);
        Assert.DoesNotContain(TargetUserId.ToString(), details);
    }

    // UT-AD-10
    [Fact]
    public void AssignRole_BuildAuditDetails_WithoutResolution_FallsBackToGuid()
    {
        var cmd = new AssignUserRoleCommand(AdminId, TargetUserId, "Manager");

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains(TargetUserId.ToString(), details);
    }

    // UT-AD-11
    [Fact]
    public void RemoveRole_BuildAuditDetails_ContainsUserNameAfterResolution()
    {
        var cmd = new RemoveUserRoleCommand(AdminId, TargetUserId, "SystemAdmin")
        {
            ResolvedTargetUserName = "Beatriz Sanz"
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("SystemAdmin", details);
        Assert.Contains("Beatriz Sanz", details);
        Assert.DoesNotContain(TargetUserId.ToString(), details);
    }

    // UT-AD-12
    [Fact]
    public void RemoveRole_BuildAuditDetails_WithoutResolution_FallsBackToGuid()
    {
        var cmd = new RemoveUserRoleCommand(AdminId, TargetUserId, "SystemAdmin");

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains(TargetUserId.ToString(), details);
    }
}
