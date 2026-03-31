using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.Reservations;

/// <summary>
/// Tests de integración para <see cref="CancelReservationCommand"/>.
/// Verifican que la cancelación persiste correctamente y que se aplican
/// todas las reglas de propiedad y estado.
/// </summary>
public sealed class CancelReservationIntegrationTests : IntegrationTestBase
{
    // ── IT-CA-01 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CancelReservation_OwnActiveReservation_SetsStatusCancelled()
    {
        // Arrange: crear reserva activa para TestUser
        var reservation = new Reservation(Guid.NewGuid(), TestUserId, DockNA01, Today, TimeSlot.Morning);
        DbContext.Reservations.Add(reservation);
        await DbContext.SaveChangesAsync();

        var command = new CancelReservationCommand(reservation.Id, TestUserId);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var saved = await DbContext.Reservations.SingleAsync(r => r.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Cancelled, saved.Status);
    }

    // ── IT-CA-02 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CancelReservation_NotOwner_ReturnsFailure()
    {
        // Arrange: reserva de TestUser; intenta cancelar AnotherUser
        var reservation = new Reservation(Guid.NewGuid(), TestUserId, DockNA01, Today, TimeSlot.Afternoon);
        DbContext.Reservations.Add(reservation);
        await DbContext.SaveChangesAsync();

        var command = new CancelReservationCommand(reservation.Id, AnotherUserId);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.NotReservationOwner, result.Error);

        // La reserva debe seguir activa
        var saved = await DbContext.Reservations.SingleAsync(r => r.Id == reservation.Id);
        Assert.Equal(ReservationStatus.Active, saved.Status);
    }

    // ── IT-CA-03 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CancelReservation_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange: crear reserva y cancelarla directamente en BD
        var reservation = new Reservation(Guid.NewGuid(), TestUserId, DockNA01, Today, TimeSlot.Both);
        reservation.Cancel();
        DbContext.Reservations.Add(reservation);
        await DbContext.SaveChangesAsync();

        var command = new CancelReservationCommand(reservation.Id, TestUserId);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.AlreadyCancelled, result.Error);
    }

    // ── IT-CA-04 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CancelReservation_ReservationNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = new CancelReservationCommand(nonExistentId, TestUserId);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.ReservationNotFound, result.Error);
    }
}
