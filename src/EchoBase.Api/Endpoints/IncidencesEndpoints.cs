using EchoBase.Api.Services;
using EchoBase.Core.Incidences.Commands;
using EchoBase.Core.Incidences.Queries;
using MediatR;

namespace EchoBase.Api.Endpoints;

/// <summary>
/// Endpoints REST para gestión de incidencias en puestos de trabajo.
/// </summary>
internal static class IncidencesEndpoints
{
    /// <summary>
    /// Registra todos los endpoints del grupo de incidencias en el pipeline de la aplicación.
    /// </summary>
    public static IEndpointRouteBuilder MapIncidencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/incidences").RequireAuthorization();

        group.MapPost("/", ReportIncidenceAsync)
            .WithName("ReportIncidence")
            .WithSummary("Reporta una incidencia en un puesto de trabajo");

        group.MapGet("/mine", GetMyIncidencesAsync)
            .WithName("GetMyIncidences")
            .WithSummary("Obtiene las incidencias reportadas por el usuario autenticado");

        return app;
    }

    private static async Task<IResult> ReportIncidenceAsync(
        [Microsoft.AspNetCore.Mvc.FromBody] ReportIncidenceRequest request,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new ReportIncidenceCommand(currentUser.UserId, request.DockId, request.Description);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new { error = result.Error });
        return Results.Created($"/api/v1/incidences/{result.Value}", new { id = result.Value });
    }

    private static async Task<IResult> GetMyIncidencesAsync(
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var query = new GetUserIncidencesQuery(currentUser.UserId);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }
}

/// <summary>
/// Cuerpo de la petición para reportar una incidencia.
/// </summary>
/// <param name="DockId">Identificador del puesto afectado.</param>
/// <param name="Description">Descripción de la incidencia.</param>
internal sealed record ReportIncidenceRequest(Guid DockId, string Description);
