using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.SystemAdmin;
using EchoBase.Core.SystemAdmin.Commands;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.SystemAdmin;

/// <summary>
/// Tests de integración para <see cref="BulkCancelReservationsCommand"/>.
/// </summary>
public sealed class BulkCancelReservationsIntegrationTests : IntegrationTestBase
{
    private async Task<Guid> CreateActiveReservation(Guid userId, Guid dockId, DateOnly date, TimeSlot slot)
    {
        var reservation = new Reservation(Guid.NewGuid(), userId, dockId, date, slot);
        DbContext.Reservations.Add(reservation);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        return reservation.Id;
    }

    // ── IT-BC-01 ──────────────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_ActiveReservationsInRange_CancelsAll()
    {
        // Arrange
        var r1 = await CreateActiveReservation(TestUserId, DockNA01, Today, TimeSlot.Morning);
        var r2 = await CreateActiveReservation(AnotherUserId, DockNA02, Today.AddDays(1), TimeSlot.Afternoon);

        // Act
        var result = await Mediator.Send(new BulkCancelReservationsCommand(
            AdminUserId, Today, Today.AddDays(1), "Cierre por obras", null));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.CancelledCount);

        var res1 = await DbContext.Reservations.SingleAsync(r => r.Id == r1);
        var res2 = await DbContext.Reservations.SingleAsync(r => r.Id == r2);
        Assert.Equal(ReservationStatus.Cancelled, res1.Status);
        Assert.Equal(ReservationStatus.Cancelled, res2.Status);
    }

    // ── IT-BC-02 ──────────────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_SpecificDocks_OnlyCancelsMatchingDocks()
    {
        // Arrange
        var r1 = await CreateActiveReservation(TestUserId, DockNA01, Today, TimeSlot.Morning);
        var r2 = await CreateActiveReservation(AnotherUserId, DockNA02, Today, TimeSlot.Morning);

        // Act: cancel only DockNA01
        var result = await Mediator.Send(new BulkCancelReservationsCommand(
            AdminUserId, Today, Today, "Solo este puesto", new List<Guid> { DockNA01 }));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.CancelledCount);

        var res1 = await DbContext.Reservations.SingleAsync(r => r.Id == r1);
        var res2 = await DbContext.Reservations.SingleAsync(r => r.Id == r2);
        Assert.Equal(ReservationStatus.Cancelled, res1.Status);
        Assert.Equal(ReservationStatus.Active, res2.Status);
    }

    // ── IT-BC-03 ──────────────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_NoReservationsInRange_ReturnsZeroCount()
    {
        // Act: range in the past (no reservations exist there)
        var result = await Mediator.Send(new BulkCancelReservationsCommand(
            AdminUserId, Today.AddDays(-5), Today.AddDays(-1), "Rango vacío", null));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.CancelledCount);
    }

    // ── IT-BC-04 ──────────────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_InvalidDateRange_ReturnsError()
    {
        var result = await Mediator.Send(new BulkCancelReservationsCommand(
            AdminUserId, Today.AddDays(3), Today, "Rango inválido", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.InvalidDateRange, result.Error);
    }

    // ── IT-BC-05 ──────────────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_NonSystemAdmin_ReturnsError()
    {
        var result = await Mediator.Send(new BulkCancelReservationsCommand(
            TestUserId, Today, Today, "Sin permiso", null));

        Assert.False(result.IsSuccess);
        Assert.Equal(SystemAdminErrors.NotSystemAdmin, result.Error);
    }

    // ── IT-BC-06: AuditLog ────────────────────────────────────────
    [Fact]
    public async Task BulkCancel_WithReservations_WritesAuditLog()
    {
        await CreateActiveReservation(TestUserId, DockNA01, Today, TimeSlot.Morning);

        await Mediator.Send(new BulkCancelReservationsCommand(
            AdminUserId, Today, Today, "Prueba de auditoría", null));

        var log = await DbContext.AuditLogs.SingleOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(AuditAction.BulkReservationsCancelled, log.Action);
    }
}
