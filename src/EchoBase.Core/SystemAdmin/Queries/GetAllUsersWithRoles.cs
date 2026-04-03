using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.SystemAdmin.Queries;

/// <summary>
/// Consulta que devuelve todos los usuarios del sistema con sus roles asignados.
/// Usada por el cuadro de mando de SystemAdmin para gestión de usuarios.
/// </summary>
public sealed record GetAllUsersWithRolesQuery : IRequest<IReadOnlyList<UserWithRolesDto>>;

/// <summary>
/// Handler de <see cref="GetAllUsersWithRolesQuery"/>.
/// </summary>
public sealed class GetAllUsersWithRolesHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllUsersWithRolesQuery, IReadOnlyList<UserWithRolesDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserWithRolesDto>> Handle(
        GetAllUsersWithRolesQuery request, CancellationToken cancellationToken)
    {
        return await userRepository.GetAllWithRolesAsync(cancellationToken);
    }
}
