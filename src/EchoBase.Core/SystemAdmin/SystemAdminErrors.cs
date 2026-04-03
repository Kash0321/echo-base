namespace EchoBase.Core.SystemAdmin;

/// <summary>
/// Constantes de error para las operaciones del cuadro de mando de SystemAdmin.
/// </summary>
public static class SystemAdminErrors
{
    /// <summary>El solicitante no tiene el rol SystemAdmin.</summary>
    public const string NotSystemAdmin = "NOT_SYSTEM_ADMIN";

    /// <summary>El usuario objetivo no existe en el sistema.</summary>
    public const string UserNotFound = "USER_NOT_FOUND";

    /// <summary>El rol indicado no existe o no es válido.</summary>
    public const string InvalidRole = "INVALID_ROLE";

    /// <summary>El usuario ya tiene el rol que se intenta asignar.</summary>
    public const string RoleAlreadyAssigned = "ROLE_ALREADY_ASSIGNED";

    /// <summary>El usuario no tiene el rol que se intenta retirar.</summary>
    public const string RoleNotAssigned = "ROLE_NOT_ASSIGNED";

    /// <summary>El rango de fechas para la cancelación masiva no es válido.</summary>
    public const string InvalidDateRange = "INVALID_DATE_RANGE";
}
