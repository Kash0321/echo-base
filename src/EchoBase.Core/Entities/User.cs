using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Entities;

/// <summary>
/// Representa un empleado que puede autenticarse mediante Azure AD y realizar reservas de puestos de trabajo.
/// </summary>
public sealed class User(Guid id) : EntityBase
{
    /// <summary>Identificador único del usuario.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>Nombre completo del usuario, tal como aparece en Azure AD.</summary>
    public required string Name { get; init; }

    /// <summary>Dirección de correo electrónico corporativa del usuario.</summary>
    public required string Email { get; init; }

    /// <summary>Línea de negocio a la que pertenece el usuario.</summary>
    public BusinessLine BusinessLine { get; init; }

    /// <summary>Reservas realizadas por este usuario.</summary>
    public ICollection<Reservation> Reservations { get; } = new List<Reservation>();

    /// <summary>Roles de autorización asignados al usuario.</summary>
    public ICollection<Role> Roles { get; } = new List<Role>();

}
