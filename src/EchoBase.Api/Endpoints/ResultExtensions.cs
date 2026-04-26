using EchoBase.Core.Common;
using EchoBase.Core.Reservations;

namespace EchoBase.Api.Endpoints;

/// <summary>
/// Extensiones para convertir objetos <see cref="Result"/> y <see cref="Result{T}"/>
/// del dominio en respuestas HTTP adecuadas para la Minimal API.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>
    /// Errores de dominio que deben mapearse a HTTP 403 Forbidden
    /// (el recurso existe pero el solicitante no tiene permiso sobre él).
    /// </summary>
    private static readonly HashSet<string> ForbiddenErrors =
    [
        ReservationErrors.NotReservationOwner,
    ];

    /// <summary>
    /// Convierte un <see cref="Result"/> sin valor en un <see cref="IResult"/> HTTP.
    /// Éxito → 204 No Content. Fallo → 403 o 422 según el código de error.
    /// </summary>
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return ForbiddenErrors.Contains(result.Error!)
            ? Results.Forbid()
            : Results.UnprocessableEntity(new { error = result.Error });
    }

    /// <summary>
    /// Convierte un <see cref="Result{T}"/> en un <see cref="IResult"/> HTTP.
    /// Éxito → 200 OK con el valor. Fallo → 403 o 422 según el código de error.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);

        return ForbiddenErrors.Contains(result.Error!)
            ? Results.Forbid()
            : Results.UnprocessableEntity(new { error = result.Error });
    }

    /// <summary>
    /// Convierte un <see cref="Result{T}"/> en un <see cref="IResult"/> HTTP 201 Created.
    /// Éxito → 201 Created con la URI y el identificador. Fallo → 403 o 422.
    /// </summary>
    public static IResult ToCreatedHttpResult<T>(this Result<T> result, string routeName, object? routeValues = null)
    {
        if (result.IsSuccess)
            return Results.CreatedAtRoute(routeName, routeValues, result.Value);

        return ForbiddenErrors.Contains(result.Error!)
            ? Results.Forbid()
            : Results.UnprocessableEntity(new { error = result.Error });
    }
}
