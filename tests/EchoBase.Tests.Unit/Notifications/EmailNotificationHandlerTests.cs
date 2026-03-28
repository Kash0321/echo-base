using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using EchoBase.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace EchoBase.Tests.Unit.Notifications;

public class EmailNotificationHandlerTests
{
    private static readonly DateOnly TestDate = new(2026, 3, 28);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();

    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    // ──────────────────────────────────────────────────────────────
    // ReservationCreated → Email
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationCreated_SendsEmailToUser()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserContactInfo("user@test.com", "Test User"));

        var handler = new ReservationCreatedEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationCreatedEmailHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            "user@test.com",
            Arg.Is<string>(s => s.Contains("N-A01")),
            Arg.Is<string>(s => s.Contains("Mañana") && s.Contains("Test User")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationCreated_UserNotFound_DoesNotSendEmail()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((UserContactInfo?)null);

        var handler = new ReservationCreatedEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationCreatedEmailHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TimeSlot.Morning, "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both, "Mañana y Tarde")]
    public async Task ReservationCreated_IncludesCorrectSlotText(TimeSlot slot, string expectedText)
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserContactInfo("user@test.com", "Test User"));

        var handler = new ReservationCreatedEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationCreatedEmailHandler>.Instance);

        var notification = new ReservationCreatedNotification(
            ReservationId, UserId, "N-A01", TestDate, slot);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(expectedText)),
            Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // ReservationCancelled → Email
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationCancelled_SendsEmailToUser()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserContactInfo("user@test.com", "Test User"));

        var handler = new ReservationCancelledEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationCancelledEmailHandler>.Instance);

        var notification = new ReservationCancelledNotification(
            ReservationId, UserId, "D-1A01", TestDate, TimeSlot.Afternoon);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            "user@test.com",
            Arg.Is<string>(s => s.Contains("cancelada") && s.Contains("D-1A01")),
            Arg.Is<string>(s => s.Contains("Tarde") && s.Contains("Test User")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationCancelled_UserNotFound_DoesNotSendEmail()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((UserContactInfo?)null);

        var handler = new ReservationCancelledEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationCancelledEmailHandler>.Instance);

        var notification = new ReservationCancelledNotification(
            ReservationId, UserId, "D-1A01", TestDate, TimeSlot.Afternoon);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ──────────────────────────────────────────────────────────────
    // ReservationReminder → Email
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReservationReminder_SendsReminderEmailToUser()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserContactInfo("user@test.com", "Test User"));

        var handler = new ReservationReminderEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationReminderEmailHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Both);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            "user@test.com",
            Arg.Is<string>(s => s.Contains("Recordatorio") && s.Contains("N-A01")),
            Arg.Is<string>(s => s.Contains("Mañana y Tarde") && s.Contains("Test User") && s.Contains("Mis reservas")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReservationReminder_UserNotFound_DoesNotSendEmail()
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns((UserContactInfo?)null);

        var handler = new ReservationReminderEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationReminderEmailHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, TimeSlot.Morning);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TimeSlot.Morning, "Mañana")]
    [InlineData(TimeSlot.Afternoon, "Tarde")]
    [InlineData(TimeSlot.Both, "Mañana y Tarde")]
    public async Task ReservationReminder_IncludesCorrectSlotText(TimeSlot slot, string expectedText)
    {
        _userRepository.GetContactInfoAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new UserContactInfo("user@test.com", "Test User"));

        var handler = new ReservationReminderEmailHandler(
            _emailService,
            _userRepository,
            NullLogger<ReservationReminderEmailHandler>.Instance);

        var notification = new ReservationReminderNotification(
            ReservationId, UserId, "N-A01", TestDate, slot);

        await handler.Handle(notification, CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains(expectedText)),
            Arg.Any<CancellationToken>());
    }
}
