using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;

namespace EchoBase.Core.Interfaces;

/// <summary>
/// Información básica de un usuario para notificaciones.
/// </summary>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Name">Nombre completo del usuario.</param>
public sealed record UserContactInfo(string Email, string Name);

/// <summary>
/// DTO de perfil de usuario con todos los campos consultables y editables.
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    BusinessLine BusinessLine,
    string? PhoneNumber,
    bool EmailNotifications,
    bool TeamsNotifications);

/// <summary>
/// Abstracción para obtener y actualizar datos de usuarios.
/// </summary>
public interface IUserRepository
{
    /// <summary>Obtiene la información de contacto de un usuario por su identificador.</summary>
    Task<UserContactInfo?> GetContactInfoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Garantiza que el usuario existe en la base de datos.
    /// Si no existe, lo crea con los datos proporcionados.
    /// </summary>
    Task EnsureUserAsync(Guid userId, string name, string email, CancellationToken ct = default);

    /// <summary>Obtiene el perfil completo de un usuario para su visualización y edición.</summary>
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Obtiene la entidad <see cref="User"/> rastreada por EF Core para su actualización.</summary>
    Task<User?> GetForUpdateAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Persiste los cambios pendientes en la unidad de trabajo.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
