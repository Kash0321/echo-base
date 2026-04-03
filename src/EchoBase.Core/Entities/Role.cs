namespace EchoBase.Core.Entities;

/// <summary>
/// Rol de autorización que determina las acciones permitidas a los usuarios dentro de la aplicación.
/// </summary>
/// <remarks>
/// Los roles predefinidos son <c>BasicUser</c> (solo puede reservar puestos)
/// y <c>Manager</c> (puede reservar y bloquear puestos de trabajo).
/// Los roles se gestionan en Azure AD y se sincronizan al iniciar sesión.
/// </remarks>
public sealed class Role(Guid id) : EntityBase
{
    /// <summary>Identificador único del rol.</summary>
    public Guid Id { get; } = EnsureValidId(id);

    /// <summary>
    /// Nombre del rol. Los valores válidos son <c>BasicUser</c> y <c>Manager</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>Usuarios que tienen asignado este rol.</summary>
    public ICollection<User> Users { get; } = new List<User>();

}
