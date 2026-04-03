using EchoBase.Core.Common;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Incidences.Notifications;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Incidences.Commands;

/// <summary>
/// Comando para actualizar el estado de un reporte de incidencia.
/// Solo los usuarios con rol <c>Manager</c> pueden ejecutar esta acción.
/// </summary>
public sealed record UpdateIncidenceStatusCommand(
    Guid ManagerUserId,
    Guid IncidenceId,
    IncidenceStatus NewStatus,
    string? Comment) : IRequest<Result>, IAuditableRequest
{
    internal string? ResolvedDockCode { get; set; }

    Guid? IAuditableRequest.PerformedByUserId => ManagerUserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.IncidenceStatusUpdated;

    string IAuditableRequest.BuildAuditDetails()
    {
        var statusLabel = NewStatus switch
        {
            IncidenceStatus.Open        => "Abierta",
            IncidenceStatus.UnderReview => "En revisión",
            IncidenceStatus.Resolved    => "Resuelta",
            IncidenceStatus.Rejected    => "Rechazada",
            _                           => NewStatus.ToString()
        };
        return $"Incidencia en puesto '{ResolvedDockCode ?? IncidenceId.ToString()}' → {statusLabel}"
             + (string.IsNullOrWhiteSpace(Comment) ? "" : $" — Comentario: {Comment}");
    }
}

/// <summary>
/// Handler que procesa la actualización del estado de una incidencia.
/// </summary>
public sealed class UpdateIncidenceStatusHandler(
    IBlockedDockRepository blockedDockRepository,
    IIncidenceRepository incidenceRepository,
    IPublisher publisher,
    TimeProvider timeProvider)
    : IRequestHandler<UpdateIncidenceStatusCommand, Result>
{
    private const string ManagerRole = "Manager";

    public async Task<Result> Handle(UpdateIncidenceStatusCommand request, CancellationToken cancellationToken)
    {
        if (!await blockedDockRepository.UserHasRoleAsync(request.ManagerUserId, ManagerRole, cancellationToken))
            return Result.Failure(IncidenceErrors.NotManager);

        var incidence = await incidenceRepository.GetByIdAsync(request.IncidenceId, cancellationToken);
        if (incidence is null)
            return Result.Failure(IncidenceErrors.IncidenceNotFound);

        var dockCode = await incidenceRepository.GetDockCodeAsync(incidence.DockId, cancellationToken);
        request.ResolvedDockCode = dockCode;

        var now = timeProvider.GetUtcNow();
        incidence.UpdateStatus(request.NewStatus, request.ManagerUserId, now, request.Comment);

        await incidenceRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new IncidenceStatusUpdatedNotification(
            incidence.Id,
            dockCode ?? incidence.DockId.ToString(),
            incidence.ReportedByUserId,
            request.NewStatus,
            request.Comment), cancellationToken);

        return Result.Success();
    }
}
