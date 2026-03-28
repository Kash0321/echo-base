using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EchoBase.Tests.Unit.Notifications;

public class TeamsNotificationHandlerTests
{
    private static readonly DateOnly TestDate = new(2026, 3, 28);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();

    private readonly ITeamsNotificationService _teamsService = Substitute.For<ITeamsNotificationService>();

    // ──────────────────────────────────────────────────────────────
    // ReservationCreated → Teams
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationCreated_SendsTeamsMessage()
    {
        var handler = new ReservationCreatedTeamsHandler(
            _teamsService,
            NullLogger<ReservationCreatedTeamsHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        await handler.Handle(notification, CancellationToken.None);

        await _teamsService.Received(1).SendChatMessageAsync(
            UserId.ToString(),
            Arg.Is<string>(s => s.Contains("N-A01") && s.Contains("Mañana")),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TimeSlot.Morning, "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both, "Mañana y Tarde")]
    public async Task ReservationCreated_IncludesCorrectSlotText(TimeSlot slot, string expectedText)
    {
        var handler = new ReservationCreatedTeamsHandler(
            _teamsService,
            NullLogger<ReservationCreatedTeamsHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, slot);

        await handler.Handle(notification, CancellationToken.None);

        await _teamsService.Received(1).SendChatMessageAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(expectedText)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationCreated_ServiceThrows_DoesNotPropagate()
    {
        _teamsService.SendChatMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Teams API down"));

        var handler = new ReservationCreatedTeamsHandler(
            _teamsService,
            NullLogger<ReservationCreatedTeamsHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        // Should not throw
        await handler.Handle(notification, CancellationToken.None);
    }

    // ──────────────────────────────────────────────────────────────
    // ReservationCancelled → Teams
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationCancelled_SendsTeamsMessage()
    {
        var handler = new ReservationCancelledTeamsHandler(
            _teamsService,
            NullLogger<ReservationCancelledTeamsHandler>.Instance);

        var notification = new ReservationCancelledNotification(
            ReservationId, UserId, "D-2B01", TestDate, TimeSlot.Both);

        await handler.Handle(notification, CancellationToken.None);

        await _teamsService.Received(1).SendChatMessageAsync(
            UserId.ToString(),
            Arg.Is<string>(s => s.Contains("D-2B01") && s.Contains("cancelada")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationCancelled_ServiceThrows_DoesNotPropagate()
    {
        _teamsService.SendChatMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Teams API down"));

        var handler = new ReservationCancelledTeamsHandler(
            _teamsService,
            NullLogger<ReservationCancelledTeamsHandler>.Instance);

        var notification = new ReservationCancelledNotification(
            ReservationId, UserId, "D-2B01", TestDate, TimeSlot.Both);

        // Should not throw
        await handler.Handle(notification, CancellationToken.None);
    }

    // ──────────────────────────────────────────────────────────────
    // ReservationReminder → Teams
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationReminder_SendsTeamsMessage()
    {
        var handler = new ReservationReminderTeamsHandler(
            _teamsService,
            NullLogger<ReservationReminderTeamsHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        await handler.Handle(notification, CancellationToken.None);

        await _teamsService.Received(1).SendChatMessageAsync(
            UserId.ToString(),
            Arg.Is<string>(s => s.Contains("N-A01") && s.Contains("Recordatorio") && s.Contains("Mañana")),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TimeSlot.Morning, "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both, "Mañana y Tarde")]
    public async Task ReservationReminder_IncludesCorrectSlotText(TimeSlot slot, string expectedText)
    {
        var handler = new ReservationReminderTeamsHandler(
            _teamsService,
            NullLogger<ReservationReminderTeamsHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, slot);

        await handler.Handle(notification, CancellationToken.None);

        await _teamsService.Received(1).SendChatMessageAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(expectedText)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationReminder_ServiceThrows_DoesNotPropagate()
    {
        _teamsService.SendChatMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Teams API down"));

        var handler = new ReservationReminderTeamsHandler(
            _teamsService,
            NullLogger<ReservationReminderTeamsHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        // Should not throw
        await handler.Handle(notification, CancellationToken.None);
    }
}
