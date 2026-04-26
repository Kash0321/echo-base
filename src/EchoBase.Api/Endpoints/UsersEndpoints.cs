using EchoBase.Api.Services;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Users.Commands;
using EchoBase.Core.Users.Queries;
using MediatR;

namespace EchoBase.Api.Endpoints;

/// <summary>
/// Endpoints REST para gestión del perfil del usuario autenticado.
/// </summary>
internal static class UsersEndpoints
{
    /// <summary>
    /// Registra todos los endpoints del grupo de usuarios en el pipeline de la aplicación.
    /// </summary>
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").RequireAuthorization();

        group.MapGet("/me", GetMyProfileAsync)
            .WithName("GetMyProfile")
            .WithSummary("Obtiene el perfil del usuario autenticado");

        group.MapPut("/me", UpdateMyProfileAsync)
            .WithName("UpdateMyProfile")
            .WithSummary("Actualiza los datos editables del perfil del usuario autenticado");

        return app;
    }

    private static async Task<IResult> GetMyProfileAsync(
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var query = new GetUserProfileQuery(currentUser.UserId);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateMyProfileAsync(
        [Microsoft.AspNetCore.Mvc.FromBody] UpdateProfileRequest request,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new UpdateUserProfileCommand(
            currentUser.UserId,
            request.BusinessLine,
            request.PhoneNumber,
            request.EmailNotifications,
            request.TeamsNotifications);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }
}

/// <summary>
/// Cuerpo de la petición para actualizar el perfil de usuario.
/// </summary>
/// <param name="BusinessLine">Nueva línea de negocio.</param>
/// <param name="PhoneNumber">Número de teléfono de contacto (opcional).</param>
/// <param name="EmailNotifications">Habilitar notificaciones por correo.</param>
/// <param name="TeamsNotifications">Habilitar notificaciones por Teams.</param>
internal sealed record UpdateProfileRequest(
    BusinessLine BusinessLine,
    string? PhoneNumber,
    bool EmailNotifications,
    bool TeamsNotifications);
