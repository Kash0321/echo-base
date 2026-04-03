using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.SystemAdmin;

/// <summary>
/// Tests de integración para <see cref="CreateEmergencyReservationCommand"/>.
/// </summary>
public sealed class CreateEmergencyReservationIntegrationTests : IntegrationTestBase
{
    // ── IT-ER-01 ──────────────────────────────────────────────────
    [Fact]
    public async Task CreateEmergencyReservation_Valid_PersistsForTargetUser()
    {
        var command = new CreateEmergencyReservationCommand(
            AdminUserId, TestUserId, DockNA01, Today, TimeSlot.Morning);

        var result = await Mediator.Send(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var saved = await DbContext.Reservations.SingleOrDefaultAsync(r => r.Id == result.Value);
        Assert.NotNull(saved);
        Assert.Equal(TestUserId, saved.UserId);     // belongs to target user, not admin
        Assert.Equal(DockNA01, saved.DockId);
        Assert.Equal(Today, saved.Date);
        Assert.Equal(TimeSlot.Morning, saved.TimeSlot);
        Assert.Equal(ReservationStatus.Active, saved.Status);
    }

    // ── IT-ER-02 ──────────────────────────────────────────────────
    [Fact]
    public async Task CreateEmergencyReservation_NonSystemAdmin_ReturnsError()
    {
        var command = new CreateEmergencyReservationCommand(
            TestUserId, AnotherUserId, DockNA01, Today, TimeSlot.Morning);

        var result = await Mediator.Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ── IT-ER-03 ──────────────────────────────────────────────────
    [Fact]
    public async Task CreateEmergencyReservation_DateInPast_ReturnsError()
    {
        var command = new CreateEmergencyReservationCommand(
            AdminUserId, TestUserId, DockNA01, Today.AddDays(-1), TimeSlot.Morning);

        var result = await Mediator.Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateInThePast, result.Error);
    }

    // ── IT-ER-04 ──────────────────────────────────────────────────
    [Fact]
    public async Task CreateEmergencyReservation_DockAlreadyBooked_ReturnsDockNotAvailable()
    {
        // Arrange: book the dock first via normal command
        DbContext.Reservations.Add(new EchoBase.Core.Entities.Reservation(
            Guid.NewGuid(), AnotherUserId, DockNA01, Today, TimeSlot.Morning));
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        // Act: emergency reservation on same dock/date/slot
        var command = new CreateEmergencyReservationCommand(
            AdminUserId, TestUserId, DockNA01, Today, TimeSlot.Morning);

        var result = await Mediator.Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    // ── IT-ER-05: AuditLog ────────────────────────────────────────
    [Fact]
    public async Task CreateEmergencyReservation_Valid_WritesAuditLog()
    {
        var command = new CreateEmergencyReservationCommand(
            AdminUserId, TestUserId, DockNA01, Today, TimeSlot.Morning);

        await Mediator.Send(command);

        var log = await DbContext.AuditLogs.SingleOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(AuditAction.EmergencyReservationCreated, log.Action);
        Assert.Equal(AdminUserId, log.PerformedByUserId);
    }
}
