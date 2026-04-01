using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.Reservations;

/// <summary>
/// Tests de integración para <see cref="CreateReservationCommand"/>.
/// Verifican la secuencia completa: comando → handler → repositorio → SQLite.
///
/// Cada clase de test ejecuta sobre su propia base de datos in-memory,
/// garantizando aislamiento total entre suites.
/// </summary>
public sealed class CreateReservationIntegrationTests : IntegrationTestBase
{
    // ── IT-CR-01 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_ValidRequest_PersistsReservationAndReturnsId()
    {
        // Arrange
        var command = new CreateReservationCommand(TestUserId, DockNA01, Today, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var saved = await DbContext.Reservations.SingleOrDefaultAsync(r => r.Id == result.Value);
        Assert.NotNull(saved);
        Assert.Equal(TestUserId, saved.UserId);
        Assert.Equal(DockNA01, saved.DockId);
        Assert.Equal(Today, saved.Date);
        Assert.Equal(TimeSlot.Morning, saved.TimeSlot);
        Assert.Equal(ReservationStatus.Active, saved.Status);
    }

    // ── IT-CR-02 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_DateInThePast_ReturnsFailure()
    {
        // Arrange
        var yesterday = Today.AddDays(-1);
        var command = new CreateReservationCommand(TestUserId, DockNA01, yesterday, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateInThePast, result.Error);
    }

    // ── IT-CR-03 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_DateTooFarAhead_ReturnsFailure()
    {
        // Arrange
        var tooFar = Today.AddDays(8);
        var command = new CreateReservationCommand(TestUserId, DockNA01, tooFar, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DateTooFarAhead, result.Error);
    }

    // ── IT-CR-04 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_DockDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var nonExistentDockId = Guid.NewGuid();
        var command = new CreateReservationCommand(TestUserId, nonExistentDockId, Today, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotFound, result.Error);
    }

    // ── IT-CR-05 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_DockBlocked_ReturnsFailure()
    {
        // Arrange: bloquear DockNA02 para Today
        var blockedDock = new BlockedDock(
            Guid.NewGuid(),
            DockNA02,
            ManagerUserId,
            Today,
            Today,
            "Reunión de dirección");

        DbContext.BlockedDocks.Add(blockedDock);
        await DbContext.SaveChangesAsync();

        var command = new CreateReservationCommand(TestUserId, DockNA02, Today, TimeSlot.Afternoon);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockBlocked, result.Error);
    }

    // ── IT-CR-06 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_DockAlreadyFullyBooked_ReturnsFailure()
    {
        // Arrange: otro usuario reserva la franja "Both" (ocupa mañana y tarde)
        var existing = new Reservation(Guid.NewGuid(), AnotherUserId, DockNA01, Today, TimeSlot.Both);
        DbContext.Reservations.Add(existing);
        await DbContext.SaveChangesAsync();

        var command = new CreateReservationCommand(TestUserId, DockNA01, Today, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.DockNotAvailable, result.Error);
    }

    // ── IT-CR-07 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_UserExceedsMaxDailySlots_ReturnsFailure()
    {
        // Arrange: el usuario ya tiene Both (= 2 franjas) en otro puesto el mismo día
        var existing = new Reservation(Guid.NewGuid(), TestUserId, DockNB01, Today, TimeSlot.Both);
        DbContext.Reservations.Add(existing);
        await DbContext.SaveChangesAsync();

        // Intenta reservar una tercera franja en un puesto diferente
        var command = new CreateReservationCommand(TestUserId, DockNA01, Today, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ReservationErrors.UserMaxSlotsExceeded, result.Error);
    }

    // ── IT-CR-08 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_TwoUsersBookComplementarySlots_BothSucceed()
    {
        // Arrange: primer usuario reserva la mañana en DockNA01
        var firstCommand  = new CreateReservationCommand(TestUserId,    DockNA01, Today, TimeSlot.Morning);
        var secondCommand = new CreateReservationCommand(AnotherUserId, DockNA01, Today, TimeSlot.Afternoon);

        // Act
        var firstResult  = await Mediator.Send(firstCommand);
        var secondResult = await Mediator.Send(secondCommand);

        // Assert
        Assert.True(firstResult.IsSuccess,  $"Primera reserva falló: {firstResult.Error}");
        Assert.True(secondResult.IsSuccess, $"Segunda reserva falló: {secondResult.Error}");

        var count = await DbContext.Reservations
            .CountAsync(r => r.DockId == DockNA01 && r.Date == Today && r.Status == ReservationStatus.Active);
        Assert.Equal(2, count);
    }

    // ── IT-CR-09 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_UserBooksBothSlots_Succeeds()
    {
        // Arrange & Act
        var command = new CreateReservationCommand(TestUserId, DockNA01, Today, TimeSlot.Both);
        var result  = await Mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var saved = await DbContext.Reservations.SingleAsync(r => r.Id == result.Value);
        Assert.Equal(TimeSlot.Both, saved.TimeSlot);
    }

    // ── IT-CR-10 ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateReservation_ValidRequest_ReturnsUuidVersion7Id()
    {
        // Arrange
        var command = new CreateReservationCommand(TestUserId, DockNA01, Today, TimeSlot.Morning);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        // En el formato canánico xxxxxxxx-xxxx-Mxxx-..., la posición [14] es el dígito de versión
        Assert.Equal('7', result.Value.ToString()[14]);
    }
}
