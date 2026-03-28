namespace EchoBase.Core.BlockedDocks;

/// <summary>
/// Códigos de error constantes para las operaciones de bloqueo de puestos.
/// </summary>
public static class BlockedDockErrors
{
    /// <summary>El usuario no tiene el rol de Manager necesario para esta operación.</summary>
    public const string NotManager = "NOT_MANAGER";

    /// <summary>La lista de puestos de trabajo está vacía.</summary>
    public const string EmptyDockList = "EMPTY_DOCK_LIST";

    /// <summary>Uno o más puestos de trabajo no existen.</summary>
    public const string DocksNotFound = "DOCKS_NOT_FOUND";

    /// <summary>La fecha de inicio es anterior al día de hoy.</summary>
    public const string StartDateInThePast = "START_DATE_IN_THE_PAST";

    /// <summary>La fecha de fin es anterior a la fecha de inicio.</summary>
    public const string EndDateBeforeStartDate = "END_DATE_BEFORE_START_DATE";

    /// <summary>El motivo del bloqueo no puede estar vacío.</summary>
    public const string EmptyReason = "EMPTY_REASON";

    /// <summary>Uno o más puestos ya tienen bloqueos activos que se solapan con el período solicitado.</summary>
    public const string OverlappingBlocks = "OVERLAPPING_BLOCKS";

    /// <summary>Uno o más bloqueos indicados no existen.</summary>
    public const string BlocksNotFound = "BLOCKS_NOT_FOUND";

    /// <summary>Uno o más bloqueos ya han sido desactivados.</summary>
    public const string AlreadyDeactivated = "ALREADY_DEACTIVATED";
}
