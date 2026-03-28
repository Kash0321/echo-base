using EchoBase.Infrastructure.Notifications;

namespace EchoBase.Tests.Unit.Notifications;

public class ReservationReminderServiceTests
{
    // ──────────────────────────────────────────────────────────────
    // CalculateNextRun
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateNextRun_BeforeReminderTime_ReturnsSameDay()
    {
        var now = new DateTimeOffset(2026, 3, 28, 10, 0, 0, TimeSpan.Zero);
        var next = ReservationReminderService.CalculateNextRun(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void CalculateNextRun_AfterReminderTime_ReturnsNextDay()
    {
        var now = new DateTimeOffset(2026, 3, 28, 19, 0, 0, TimeSpan.Zero);
        var next = ReservationReminderService.CalculateNextRun(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void CalculateNextRun_ExactlyAtReminderTime_ReturnsNextDay()
    {
        var now = new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero);
        var next = ReservationReminderService.CalculateNextRun(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void CalculateNextRun_Midnight_ReturnsSameDay()
    {
        var now = new DateTimeOffset(2026, 3, 28, 0, 0, 0, TimeSpan.Zero);
        var next = ReservationReminderService.CalculateNextRun(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void CalculateNextRun_OneMinuteBeforeReminderTime_ReturnsSameDay()
    {
        var now = new DateTimeOffset(2026, 3, 28, 17, 59, 0, TimeSpan.Zero);
        var next = ReservationReminderService.CalculateNextRun(now);

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 18, 0, 0, TimeSpan.Zero), next);
    }
}
