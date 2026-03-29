namespace EchoBase.Core.Users;

/// <summary>
/// Constantes de error para las operaciones sobre el perfil de usuario.
/// </summary>
public static class UserErrors
{
    /// <summary>El usuario no existe en el sistema.</summary>
    public const string UserNotFound = "USER_NOT_FOUND";

    /// <summary>El número de teléfono supera la longitud máxima permitida.</summary>
    public const string PhoneNumberTooLong = "PHONE_NUMBER_TOO_LONG";
}
