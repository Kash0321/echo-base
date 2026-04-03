using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoBase.Infrastructure.Notifications;

/// <summary>
/// Servicio en segundo plano que envía recordatorios automáticos
/// a los usuarios con reservas para el día siguiente.
/// Se ejecuta una vez al día a las 18:00 UTC.
/// </summary>
internal sealed class ReservationReminderService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ReservationReminderService> logger) : BackgroundService
{
    /// <summary>Hora UTC a la que se envían los recordatorios (18:00).</summary>
    internal static readonly TimeOnly ReminderTime = new(18, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Servicio de recordatorios de reservas iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();
            var nextRun = CalculateNextRun(now);
            var delay = nextRun - now;

            logger.LogDebug("Próximo envío de recordatorios en {Delay} ({NextRun:u}).", delay, nextRun);
            await Task.Delay(delay, timeProvider, stoppingToken);

            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al enviar recordatorios de reservas.");
            }
        }
    }

    /// <summary>
    /// Calcula el siguiente instante de ejecución (hoy o mañana a <see cref="ReminderTime"/>).
    /// </summary>
    internal static DateTimeOffset CalculateNextRun(DateTimeOffset now)
    {
        var todayRun = new DateTimeOffset(
            now.Date.Year, now.Date.Month, now.Date.Day,
            ReminderTime.Hour, ReminderTime.Minute, 0, TimeSpan.Zero);

        return now < todayRun ? todayRun : todayRun.AddDays(1);
    }

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var tomorrow = DateOnly.FromDateTime(timeProvider.GetUtcNow().AddDays(1).DateTime);
        var reservations = await repository.GetActiveReservationsForDateAsync(tomorrow, ct);

        logger.LogInformation("Enviando {Count} recordatorios para el {Date:dd/MM/yyyy}.",
            reservations.Count, tomorrow);

        foreach (var reservation in reservations)
        {
            var dockCode = reservation.Dock?.Code ?? reservation.DockId.ToString();
            await publisher.Publish(
                new ReservationReminderNotification(
                    reservation.Id,
                    reservation.UserId,
                    dockCode,
                    reservation.Date,
                    reservation.TimeSlot),
                ct);
        }
    }
}
