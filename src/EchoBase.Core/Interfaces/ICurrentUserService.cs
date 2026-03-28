namespace EchoBase.Core.Interfaces;

/// <summary>
/// Abstracción para obtener la identidad del usuario autenticado.
/// Permite desacoplar la lógica de negocio del mecanismo de autenticación concreto.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Identificador único del usuario en el sistema.</summary>
    Guid UserId { get; }

    /// <summary>Nombre completo del usuario.</summary>
    string UserName { get; }

    /// <summary>Correo electrónico del usuario.</summary>
    string Email { get; }

    /// <summary>Indica si el usuario está autenticado.</summary>
    bool IsAuthenticated { get; }
}
