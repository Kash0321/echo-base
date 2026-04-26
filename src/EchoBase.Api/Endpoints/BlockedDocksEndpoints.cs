using EchoBase.Api.Services;
using EchoBase.Core.BlockedDocks.Commands;
using MediatR;

namespace EchoBase.Api.Endpoints;

/// <summary>
/// Endpoints REST para bloqueo y desbloqueo de puestos de trabajo.
/// Restringidos al rol <c>Manager</c>.
/// </summary>
internal static class BlockedDocksEndpoints
{
    /// <summary>
    /// Registra todos los endpoints del grupo de puestos bloqueados en el pipeline de la aplicación.
    /// </summary>
    public static IEndpointRouteBuilder MapBlockedDocksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/blocked-docks")
            .RequireAuthorization()
            .RequireAuthorization(policy => policy.RequireRole("Manager"));

        group.MapPost("/", BlockDocksAsync)
            .WithName("BlockDocks")
            .WithSummary("Bloquea uno o varios puestos de trabajo durante un período");

        group.MapDelete("/", UnblockDocksAsync)
            .WithName("UnblockDocks")
            .WithSummary("Desbloquea los bloqueos activos indicados");

        return app;
    }

    private static async Task<IResult> BlockDocksAsync(
        [Microsoft.AspNetCore.Mvc.FromBody] BlockDocksRequest request,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new BlockDocksCommand(
            currentUser.UserId,
            request.DockIds,
            request.StartDate,
            request.EndDate,
            request.Reason);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UnblockDocksAsync(
        [Microsoft.AspNetCore.Mvc.FromBody] UnblockDocksRequest request,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new UnblockDocksCommand(currentUser.UserId, request.BlockedDockIds);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }
}

/// <summary>
/// Cuerpo de la petición para bloquear puestos de trabajo.
/// </summary>
/// <param name="DockIds">Identificadores de los puestos a bloquear.</param>
/// <param name="StartDate">Fecha de inicio del bloqueo (inclusiva).</param>
/// <param name="EndDate">Fecha de fin del bloqueo (inclusiva).</param>
/// <param name="Reason">Motivo del bloqueo.</param>
internal sealed record BlockDocksRequest(
    IReadOnlyList<Guid> DockIds,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);

/// <summary>
/// Cuerpo de la petición para desbloquear puestos de trabajo.
/// </summary>
/// <param name="BlockedDockIds">Identificadores de los bloqueos activos a desactivar.</param>
internal sealed record UnblockDocksRequest(IReadOnlyList<Guid> BlockedDockIds);
