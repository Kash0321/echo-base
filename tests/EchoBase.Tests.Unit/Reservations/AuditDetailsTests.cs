using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations.Commands;

namespace EchoBase.Tests.Unit.Reservations;

/// <summary>
/// Verifica que <c>BuildAuditDetails()</c> de los comandos de reserva
/// produce texto legible con nombres y fechas en lugar de GUIDs.
/// </summary>
public class AuditDetailsReservationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DockId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 4, 1);

    // UT-AD-01
    [Theory]
    [InlineData(TimeSlot.Morning,   "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both,      "Mañana y Tarde")]
    public void CreateReservation_BuildAuditDetails_ContainsDockCodeAfterResolution(
        TimeSlot slot, string expectedSlotText)
    {
        var cmd = new CreateReservationCommand(UserId, DockId, Date, slot)
        {
            ResolvedDockCode = "N-A01"
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains("N-A01", details);
        Assert.Contains("01/04/2026", details);
        Assert.Contains(expectedSlotText, details);
        Assert.DoesNotContain(DockId.ToString(), details);
    }

    // UT-AD-02
    [Fact]
    public void CreateReservation_BuildAuditDetails_WithoutResolution_FallsBackToGuid()
    {
        var cmd = new CreateReservationCommand(UserId, DockId, Date, TimeSlot.Morning);

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains(DockId.ToString(), details);
    }

    // UT-AD-03
    [Fact]
    public void CancelReservation_BuildAuditDetails_ContainsDockCodeAfterResolution()
    {
        var reservationId = Guid.NewGuid();
        var cmd = new CancelReservationCommand(reservationId, UserId)
        {
            ResolvedAuditDetails = "Puesto N-A01 · 01/04/2026 · Mañana"
        };

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Equal("Puesto N-A01 · 01/04/2026 · Mañana", details);
        Assert.DoesNotContain(reservationId.ToString(), details);
    }

    // UT-AD-04
    [Fact]
    public void CancelReservation_BuildAuditDetails_WithoutResolution_FallsBackToReservationId()
    {
        var reservationId = Guid.NewGuid();
        var cmd = new CancelReservationCommand(reservationId, UserId);

        var details = ((IAuditableRequest)cmd).BuildAuditDetails();

        Assert.Contains(reservationId.ToString(), details);
    }
}
