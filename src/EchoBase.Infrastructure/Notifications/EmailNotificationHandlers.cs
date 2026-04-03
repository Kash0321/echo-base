using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EchoBase.Infrastructure.Notifications;

/// <summary>
/// Envía un correo electrónico cuando se crea una reserva.
/// </summary>
internal sealed class ReservationCreatedEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    ILogger<ReservationCreatedEmailHandler> logger)
    : INotificationHandler<ReservationCreatedNotification>
{
    public async Task Handle(ReservationCreatedNotification notification, CancellationToken cancellationToken)
    {
        var contact = await userRepository.GetContactInfoAsync(notification.UserId, cancellationToken);
        if (contact is null)
        {
            logger.LogWarning("No se encontró contacto para el usuario {UserId}", notification.UserId);
            return;
        }

        var subject = $"Reserva confirmada — {notification.DockCode} el {notification.Date:dd/MM/yyyy}";
        var body = $"""
            <h2>Reserva confirmada</h2>
            <p>Hola {contact.Name},</p>
            <p>Tu reserva ha sido confirmada con los siguientes detalles:</p>
            <ul>
                <li><strong>Puesto:</strong> {notification.DockCode}</li>
                <li><strong>Fecha:</strong> {notification.Date:dd/MM/yyyy}</li>
                <li><strong>Franja:</strong> {FormatSlot(notification.TimeSlot)}</li>
            </ul>
            <p>Puedes gestionar tu reserva desde la aplicación EchoBase.</p>
            """;

        await emailService.SendAsync(contact.Email, subject, body, cancellationToken);
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
/// Envía un correo electrónico cuando se cancela una reserva.
/// </summary>
internal sealed class ReservationCancelledEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    ILogger<ReservationCancelledEmailHandler> logger)
    : INotificationHandler<ReservationCancelledNotification>
{
    public async Task Handle(ReservationCancelledNotification notification, CancellationToken cancellationToken)
    {
        var contact = await userRepository.GetContactInfoAsync(notification.UserId, cancellationToken);
        if (contact is null)
        {
            logger.LogWarning("No se encontró contacto para el usuario {UserId}", notification.UserId);
            return;
        }

        var subject = $"Reserva cancelada — {notification.DockCode} el {notification.Date:dd/MM/yyyy}";
        var body = $"""
            <h2>Reserva cancelada</h2>
            <p>Hola {contact.Name},</p>
            <p>Tu reserva ha sido cancelada:</p>
            <ul>
                <li><strong>Puesto:</strong> {notification.DockCode}</li>
                <li><strong>Fecha:</strong> {notification.Date:dd/MM/yyyy}</li>
                <li><strong>Franja:</strong> {FormatSlot(notification.TimeSlot)}</li>
            </ul>
            """;

        await emailService.SendAsync(contact.Email, subject, body, cancellationToken);
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
/// Envía un correo electrónico de recordatorio cuando una reserva está próxima.
/// </summary>
internal sealed class ReservationReminderEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    ILogger<ReservationReminderEmailHandler> logger)
    : INotificationHandler<ReservationReminderNotification>
{
    public async Task Handle(ReservationReminderNotification notification, CancellationToken cancellationToken)
    {
        var contact = await userRepository.GetContactInfoAsync(notification.UserId, cancellationToken);
        if (contact is null)
        {
            logger.LogWarning("No se encontró contacto para el usuario {UserId}", notification.UserId);
            return;
        }

        var subject = $"Recordatorio de reserva — {notification.DockCode} el {notification.Date:dd/MM/yyyy}";
        var body = $"""
            <h2>Recordatorio de reserva</h2>
            <p>Hola {contact.Name},</p>
            <p>Te recordamos que tienes una reserva próxima:</p>
            <ul>
                <li><strong>Puesto:</strong> {notification.DockCode}</li>
                <li><strong>Fecha:</strong> {notification.Date:dd/MM/yyyy}</li>
                <li><strong>Franja:</strong> {FormatSlot(notification.TimeSlot)}</li>
            </ul>
            <p>Puedes modificar o cancelar tu reserva desde la sección <em>Mis reservas</em> en EchoBase.</p>
            """;

        await emailService.SendAsync(contact.Email, subject, body, cancellationToken);
    }

    private static string FormatSlot(TimeSlot slot) => slot switch
    {
        TimeSlot.Morning => "Mañana",
        TimeSlot.Afternoon => "Tarde",
        TimeSlot.Both => "Mañana y Tarde",
        _ => slot.ToString()
    };
}
