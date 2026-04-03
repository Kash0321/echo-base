using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Incidences.Notifications;
using EchoBase.Core.Interfaces;
using MediatR;

namespace EchoBase.Core.Incidences.Commands;

/// <summary>
/// Comando para reportar una incidencia en un puesto de trabajo.
/// Cualquier usuario autenticado puede reportar incidencias.
/// </summary>
public sealed record ReportIncidenceCommand(
    Guid UserId,
    Guid DockId,
    string Description) : IRequest<Result<Guid>>, IAuditableRequest
{
    internal string? ResolvedDockCode { get; set; }

    Guid? IAuditableRequest.PerformedByUserId => UserId;
    AuditAction IAuditableRequest.AuditAction => AuditAction.IncidenceReported;

    string IAuditableRequest.BuildAuditDetails() =>
        $"Incidencia reportada en puesto '{ResolvedDockCode ?? DockId.ToString()}': {Description}";
}

/// <summary>
/// Handler que procesa el reporte de una nueva incidencia.
/// </summary>
public sealed class ReportIncidenceHandler(
    IIncidenceRepository incidenceRepository,
    IPublisher publisher,
    TimeProvider timeProvider)
    : IRequestHandler<ReportIncidenceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ReportIncidenceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return Result<Guid>.Failure(IncidenceErrors.DescriptionRequired);

        if (!await incidenceRepository.DockExistsAsync(request.DockId, cancellationToken))
            return Result<Guid>.Failure(IncidenceErrors.DockNotFound);

        var dockCode = await incidenceRepository.GetDockCodeAsync(request.DockId, cancellationToken);
        request.ResolvedDockCode = dockCode;

        var incidenceId = Guid.CreateVersion7();
        var now = timeProvider.GetUtcNow();

        var report = new IncidenceReport(incidenceId, request.DockId, request.UserId, now)
        {
            Description = request.Description
        };

        await incidenceRepository.AddAsync(report, cancellationToken);
        await incidenceRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new IncidenceReportedNotification(
            incidenceId,
            dockCode ?? request.DockId.ToString(),
            request.UserId,
            request.Description), cancellationToken);

        return Result<Guid>.Success(incidenceId);
    }
}
