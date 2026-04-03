using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Incidences.Notifications;
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

// ── Funcionalidad 7: Notificaciones de incidencias ─────────────────────────

/// <summary>
/// Envía un correo electrónico a todos los Managers cuando se reporta una nueva incidencia.
/// </summary>
internal sealed class IncidenceReportedEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    ILogger<IncidenceReportedEmailHandler> logger)
    : INotificationHandler<IncidenceReportedNotification>
{
    public async Task Handle(IncidenceReportedNotification notification, CancellationToken cancellationToken)
    {
        var reporter = await userRepository.GetContactInfoAsync(notification.ReportedByUserId, cancellationToken);
        var managers = await userRepository.GetManagerContactsAsync(cancellationToken);

        if (managers.Count == 0)
        {
            logger.LogWarning("No se encontraron Managers para notificar la incidencia {IncidenceId}", notification.IncidenceId);
            return;
        }

        var reporterName = reporter?.Name ?? "un usuario";
        var subject = $"Nueva incidencia reportada — Puesto {notification.DockCode}";
        var body = $"""
            <h2>Nueva incidencia reportada</h2>
            <p>Se ha reportado una nueva incidencia en el puesto <strong>{notification.DockCode}</strong>.</p>
            <ul>
                <li><strong>Reportada por:</strong> {reporterName}</li>
                <li><strong>Descripcion:</strong> {notification.Description}</li>
            </ul>
            <p>Accede al cuadro de mando de administracion en EchoBase para revisar y gestionar esta incidencia.</p>
            """;

        foreach (var manager in managers)
        {
            await emailService.SendAsync(manager.Email, subject, body, cancellationToken);
        }
    }
}

/// <summary>
/// Envía un correo electrónico al usuario cuando un Manager actualiza el estado de su incidencia.
/// </summary>
internal sealed class IncidenceStatusUpdatedEmailHandler(
    IEmailService emailService,
    IUserRepository userRepository,
    ILogger<IncidenceStatusUpdatedEmailHandler> logger)
    : INotificationHandler<IncidenceStatusUpdatedNotification>
{
    public async Task Handle(IncidenceStatusUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var contact = await userRepository.GetContactInfoAsync(notification.ReportedByUserId, cancellationToken);
        if (contact is null)
        {
            logger.LogWarning("No se encontro contacto para el usuario {UserId}", notification.ReportedByUserId);
            return;
        }

        var statusLabel = notification.NewStatus switch
        {
            IncidenceStatus.Open        => "Abierta",
            IncidenceStatus.UnderReview => "En revision",
            IncidenceStatus.Resolved    => "Resuelta",
            IncidenceStatus.Rejected    => "Rechazada",
            _                           => notification.NewStatus.ToString()
        };

        var subject = $"Actualizacion de incidencia — Puesto {notification.DockCode}";
        var body = $"""
            <h2>Tu incidencia ha sido actualizada</h2>
            <p>Hola {contact.Name},</p>
            <p>El estado de tu incidencia en el puesto <strong>{notification.DockCode}</strong> ha sido actualizado:</p>
            <ul>
                <li><strong>Nuevo estado:</strong> {statusLabel}</li>
            </ul>
            <p>Puedes ver el estado de tu incidencia en la seccion <em>Incidencias</em> de EchoBase.</p>
            """;

        await emailService.SendAsync(contact.Email, subject, body, cancellationToken);
    }
}
