using EchoBase.Core.Common;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Users.Queries;

/// <summary>
/// Consulta que devuelve el perfil completo del usuario autenticado.
/// </summary>
/// <param name="UserId">Identificador del usuario cuyo perfil se solicita.</param>
public sealed record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

/// <summary>
/// Handler de <see cref="GetUserProfileQuery"/>.
/// </summary>
public sealed class GetUserProfileHandler(IUserRepository repository)
    : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    /// <inheritdoc />
    public async Task<Result<UserProfileDto>> Handle(
        GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(request.UserId, cancellationToken);

        return profile is null
            ? Result<UserProfileDto>.Failure(UserErrors.UserNotFound)
            : Result<UserProfileDto>.Success(profile);
    }
}
