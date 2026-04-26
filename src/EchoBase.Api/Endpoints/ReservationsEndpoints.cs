using EchoBase.Api.Services;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Core.Reservations.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EchoBase.Api.Endpoints;

/// <summary>
/// Endpoints REST para gestión de reservas y consulta del mapa de puestos.
/// </summary>
internal static class ReservationsEndpoints
{
    /// <summary>
    /// Registra todos los endpoints del grupo de reservas en el pipeline de la aplicación.
    /// </summary>
    public static IEndpointRouteBuilder MapReservationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").RequireAuthorization();

        // Mapa de puestos
        group.MapGet("/docks/map", GetDockMapAsync)
            .WithName("GetDockMap")
            .WithSummary("Obtiene el mapa de puestos con su estado para una fecha");

        // Reservas del usuario
        group.MapGet("/reservations", GetMyReservationsAsync)
            .WithName("GetMyReservations")
            .WithSummary("Obtiene las reservas del usuario autenticado");

        group.MapPost("/reservations", CreateReservationAsync)
            .WithName("CreateReservation")
            .WithSummary("Crea una nueva reserva de puesto de trabajo");

        group.MapDelete("/reservations/{id:guid}", CancelReservationAsync)
            .WithName("CancelReservation")
            .WithSummary("Cancela una reserva del usuario autenticado");

        return app;
    }

    private static async Task<IResult> GetDockMapAsync(
        [FromQuery] DateOnly? date,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var query = new GetDockMapQuery(date ?? DateOnly.FromDateTime(DateTime.Today));
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMyReservationsAsync(
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var query = new GetUserReservationsQuery(currentUser.UserId);
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateReservationAsync(
        [Microsoft.AspNetCore.Mvc.FromBody] CreateReservationRequest request,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new CreateReservationCommand(
            currentUser.UserId,
            request.DockId,
            request.Date,
            request.TimeSlot);
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new { error = result.Error });
        return Results.CreatedAtRoute("GetMyReservations", null, new { id = result.Value });
    }

    private static async Task<IResult> CancelReservationAsync(
        Guid id,
        ApiCurrentUserService currentUser,
        ISender sender,
        CancellationToken ct)
    {
        await currentUser.EnsureUserAsync(ct);
        var command = new CancelReservationCommand(id, currentUser.UserId);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }
}

/// <summary>
/// Cuerpo de la petición para crear una reserva.
/// </summary>
/// <param name="DockId">Identificador del puesto de trabajo.</param>
/// <param name="Date">Fecha de la reserva.</param>
/// <param name="TimeSlot">Franja horaria solicitada.</param>
internal sealed record CreateReservationRequest(Guid DockId, DateOnly Date, TimeSlot TimeSlot);
