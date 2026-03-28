namespace EchoBase.Core.Interfaces;

/// <summary>
/// Información básica de un usuario para notificaciones.
/// </summary>
/// <param name="Email">Correo electrónico del usuario.</param>
/// <param name="Name">Nombre completo del usuario.</param>
public sealed record UserContactInfo(string Email, string Name);

/// <summary>
/// Abstracción para obtener datos de contacto de usuarios.
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
}
