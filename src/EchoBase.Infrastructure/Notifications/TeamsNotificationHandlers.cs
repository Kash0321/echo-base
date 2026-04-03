using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EchoBase.Infrastructure.Notifications;

/// <summary>
/// Envía un mensaje de Teams cuando se crea una reserva.
/// </summary>
internal sealed class ReservationCreatedTeamsHandler(
    ITeamsNotificationService teamsService,
    ILogger<ReservationCreatedTeamsHandler> logger)
    : INotificationHandler<ReservationCreatedNotification>
{
    public async Task Handle(ReservationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var message = $"""
            <b>✅ Reserva confirmada</b><br/>
            <b>Puesto:</b> {notification.DockCode}<br/>
            <b>Fecha:</b> {notification.Date:dd/MM/yyyy}<br/>
            <b>Franja:</b> {FormatSlot(notification.TimeSlot)}
            """;

        try
        {
            await teamsService.SendChatMessageAsync(
                notification.UserId.ToString(),
                message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar notificación de Teams para reserva {ReservationId}",
                notification.ReservationId);
        }
    }

    private static string FormatSlot(TimeSlot slot) => slot switch
    {
        TimeSlot.Morning => "Mañana",
        TimeSlot.Afternoon => "Tarde",
        TimeSlot.Both => "Mañana y Tarde",
        _ => slot.ToString()
    };
}

/// <summary>
/// Envía un mensaje de Teams cuando se cancela una reserva.
/// </summary>
internal sealed class ReservationCancelledTeamsHandler(
    ITeamsNotificationService teamsService,
    ILogger<ReservationCancelledTeamsHandler> logger)
    : INotificationHandler<ReservationCancelledNotification>
{
    public async Task Handle(ReservationCancelledNotification notification, CancellationToken cancellationToken)
    {
        var message = $"""
            <b>❌ Reserva cancelada</b><br/>
            <b>Puesto:</b> {notification.DockCode}<br/>
            <b>Fecha:</b> {notification.Date:dd/MM/yyyy}<br/>
            <b>Franja:</b> {FormatSlot(notification.TimeSlot)}
            """;

        try
        {
            await teamsService.SendChatMessageAsync(
                notification.UserId.ToString(),
                message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar notificación de Teams para cancelación {ReservationId}",
                notification.ReservationId);
        }
    }

    private static string FormatSlot(TimeSlot slot) => slot switch
    {
        TimeSlot.Morning => "Mañana",
        TimeSlot.Afternoon => "Tarde",
        TimeSlot.Both => "Mañana y Tarde",
        _ => slot.ToString()
    };
}

/// <summary>
/// Envía un mensaje de Teams como recordatorio de una reserva próxima.
/// </summary>
internal sealed class ReservationReminderTeamsHandler(
    ITeamsNotificationService teamsService,
    ILogger<ReservationReminderTeamsHandler> logger)
    : INotificationHandler<ReservationReminderNotification>
{
    public async Task Handle(ReservationReminderNotification notification, CancellationToken cancellationToken)
    {
        var message = $"""
            <b>🔔 Recordatorio de reserva</b><br/>
            <b>Puesto:</b> {notification.DockCode}<br/>
            <b>Fecha:</b> {notification.Date:dd/MM/yyyy}<br/>
            <b>Franja:</b> {FormatSlot(notification.TimeSlot)}<br/>
            <br/>
            Puedes modificar o cancelar tu reserva desde <em>Mis reservas</em> en EchoBase.
            """;

        try
        {
            await teamsService.SendChatMessageAsync(
                notification.UserId.ToString(),
                message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar recordatorio de Teams para reserva {ReservationId}",
                notification.ReservationId);
        }
    }

    private static string FormatSlot(TimeSlot slot) => slot switch
    {
        TimeSlot.Morning => "Mañana",
        TimeSlot.Afternoon => "Tarde",
        TimeSlot.Both => "Mañana y Tarde",
        _ => slot.ToString()
    };
}
