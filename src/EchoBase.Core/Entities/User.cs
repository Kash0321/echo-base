using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Representa un empleado que puede autenticarse mediante Azure AD y realizar reservas de puestos de trabajo.
/// </summary>
public sealed class User(Guid id) : EntityBase
{
    /// <summary>Identificador único del usuario.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>Nombre completo del usuario, tal como aparece en Azure AD. Gestionado por la autenticación.</summary>
    public required string Name { get; init; }

    /// <summary>Dirección de correo electrónico corporativa del usuario. Gestionada por la autenticación.</summary>
    public required string Email { get; init; }

    /// <summary>Línea de negocio a la que pertenece el usuario.</summary>
    public BusinessLine BusinessLine { get; private set; }

    /// <summary>Número de teléfono de contacto opcional del usuario.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Indica si el usuario desea recibir notificaciones por correo electrónico.</summary>
    public bool EmailNotifications { get; private set; } = true;

    /// <summary>Indica si el usuario desea recibir notificaciones por Microsoft Teams.</summary>
    public bool TeamsNotifications { get; private set; }

    /// <summary>Reservas realizadas por este usuario.</summary>
    public ICollection<Reservation> Reservations { get; } = new List<Reservation>();

    /// <summary>Roles de autorización asignados al usuario.</summary>
    public ICollection<Role> Roles { get; } = new List<Role>();

    /// <summary>
    /// Actualiza los datos editables del perfil del usuario.
    /// </summary>
    /// <param name="businessLine">Línea de negocio del usuario.</param>
    /// <param name="phoneNumber">Número de teléfono de contacto (puede ser <see langword="null"/>).</param>
    public void UpdateProfile(BusinessLine businessLine, string? phoneNumber)
    {
        BusinessLine = businessLine;
        PhoneNumber = phoneNumber?.Trim() is { Length: > 0 } trimmed ? trimmed : null;
    }

    /// <summary>
    /// Actualiza las preferencias de notificación del usuario.
    /// </summary>
    /// <param name="emailNotifications">Habilitar notificaciones por correo electrónico.</param>
    /// <param name="teamsNotifications">Habilitar notificaciones por Microsoft Teams.</param>
    public void UpdateNotificationPreferences(bool emailNotifications, bool teamsNotifications)
    {
        EmailNotifications = emailNotifications;
        TeamsNotifications = teamsNotifications;
    }
}
