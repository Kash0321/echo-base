using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Incidences;
using EchoBase.Core.Incidences.Commands;
using EchoBase.Core.Incidences.Queries;
using EchoBase.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Tests.Integration.Incidences;

/// <summary>
/// Tests de integración para los handlers de incidencias.
/// Verifican el flujo completo: comando/consulta → handler → repositorio → SQLite.
/// Cada clase de test usa su propia base de datos in-memory aislada.
/// </summary>
public sealed class IncidenceIntegrationTests : IntegrationTestBase
{
    // ── IT-IR-01  ReportIncidence: happy path, persiste en BD ────────────────
    [Fact]
    public async Task ReportIncidence_ValidRequest_PersistsReportAndReturnsId()
    {
        var command = new ReportIncidenceCommand(TestUserId, DockNA01, "Monitor parpadeante");

        var result = await Mediator.Send(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var saved = await DbContext.IncidenceReports.SingleOrDefaultAsync(r => r.Id == result.Value);
        Assert.NotNull(saved);
        Assert.Equal(TestUserId, saved.ReportedByUserId);
        Assert.Equal(DockNA01, saved.DockId);
        Assert.Equal("Monitor parpadeante", saved.Description);
        Assert.Equal(IncidenceStatus.Open, saved.Status);
        Assert.Null(saved.UpdatedAt);
    }

    // ── IT-IR-02  ReportIncidence: descripción vacía → fallo ─────────────────
    [Fact]
    public async Task ReportIncidence_EmptyDescription_ReturnsFailureAndDoesNotPersist()
    {
        var command = new ReportIncidenceCommand(TestUserId, DockNA01, "   ");

        var result = await Mediator.Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.DescriptionRequired, result.Error);
        Assert.False(await DbContext.IncidenceReports.AnyAsync());
    }

    // ── IT-IR-03  ReportIncidence: puesto inexistente → fallo ────────────────
    [Fact]
    public async Task ReportIncidence_UnknownDock_ReturnsDockNotFoundAndDoesNotPersist()
    {
        var unknownDock = Guid.NewGuid();
        var command = new ReportIncidenceCommand(TestUserId, unknownDock, "Silla defectuosa");

        var result = await Mediator.Send(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.DockNotFound, result.Error);
        Assert.False(await DbContext.IncidenceReports.AnyAsync());
    }

    // ── IT-IR-04  UpdateIncidenceStatus: happy path, actualiza estado ─────────
    [Fact]
    public async Task UpdateIncidenceStatus_ManagerUpdatesStatus_PersistsNewStatus()
    {
        // Crear incidencia previa
        var reportCmd = new ReportIncidenceCommand(TestUserId, DockNA01, "Ratón sin cable");
        var reportResult = await Mediator.Send(reportCmd);
        Assert.True(reportResult.IsSuccess);

        var updateCmd = new UpdateIncidenceStatusCommand(
            ManagerUserId,
            reportResult.Value,
            IncidenceStatus.UnderReview,
            "Revisando el inventario");

        var result = await Mediator.Send(updateCmd);

        Assert.True(result.IsSuccess);

        var saved = await DbContext.IncidenceReports.SingleAsync(r => r.Id == reportResult.Value);
        Assert.Equal(IncidenceStatus.UnderReview, saved.Status);
        Assert.Equal(ManagerUserId, saved.UpdatedByUserId);
        Assert.Equal("Revisando el inventario", saved.ManagerComment);
        Assert.NotNull(saved.UpdatedAt);
    }

    // ── IT-IR-05  UpdateIncidenceStatus: usuario sin rol Manager → fallo ──────
    [Fact]
    public async Task UpdateIncidenceStatus_RegularUserCannotUpdateStatus_ReturnsNotManagerError()
    {
        var reportCmd = new ReportIncidenceCommand(TestUserId, DockNA01, "Teclado sin teclas");
        var reportResult = await Mediator.Send(reportCmd);
        Assert.True(reportResult.IsSuccess);

        var updateCmd = new UpdateIncidenceStatusCommand(
            TestUserId,                // usuario sin rol Manager
            reportResult.Value,
            IncidenceStatus.Resolved,
            null);

        var result = await Mediator.Send(updateCmd);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.NotManager, result.Error);

        // Estado no debe haber cambiado
        var saved = await DbContext.IncidenceReports.SingleAsync(r => r.Id == reportResult.Value);
        Assert.Equal(IncidenceStatus.Open, saved.Status);
    }

    // ── IT-IR-06  UpdateIncidenceStatus: incidencia inexistente → fallo ───────
    [Fact]
    public async Task UpdateIncidenceStatus_UnknownIncidence_ReturnsIncidenceNotFoundError()
    {
        var fakeId = Guid.NewGuid();
        var cmd = new UpdateIncidenceStatusCommand(ManagerUserId, fakeId, IncidenceStatus.Resolved, null);

        var result = await Mediator.Send(cmd);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.IncidenceNotFound, result.Error);
    }

    // ── IT-IR-07  GetUserIncidences: devuelve solo las del usuario actual ──────
    [Fact]
    public async Task GetUserIncidences_ReturnsOnlyCurrentUserIncidences()
    {
        // TestUser reporta 2 incidencias; AnotherUser reporta 1
        await Mediator.Send(new ReportIncidenceCommand(TestUserId,    DockNA01, "Pantalla rayada"));
        await Mediator.Send(new ReportIncidenceCommand(TestUserId,    DockNA02, "Auriculares rotos"));
        await Mediator.Send(new ReportIncidenceCommand(AnotherUserId, DockNB01, "Mesa inestable"));

        var query  = new GetUserIncidencesQuery(TestUserId);
        var result = await Mediator.Send(query);

        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.NotEmpty(dto.DockCode));
    }

    // ── IT-IR-08  GetUserIncidences: usuario sin incidencias → lista vacía ────
    [Fact]
    public async Task GetUserIncidences_NoReports_ReturnsEmptyList()
    {
        var query  = new GetUserIncidencesQuery(TestUserId);
        var result = await Mediator.Send(query);

        Assert.Empty(result);
    }

    // ── IT-IR-09  GetAllIncidences: Manager ve todas las incidencias paginadas ─
    [Fact]
    public async Task GetAllIncidences_ManagerCanSeeAllIncidences()
    {
        await Mediator.Send(new ReportIncidenceCommand(TestUserId,    DockNA01, "Monitor parpadeante"));
        await Mediator.Send(new ReportIncidenceCommand(AnotherUserId, DockNA02, "Silla rota"));

        var query  = new GetAllIncidencesQuery(ManagerUserId, Page: 1, PageSize: 20);
        var result = await Mediator.Send(query);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.All(result.Value.Items, dto =>
        {
            Assert.NotEmpty(dto.DockCode);
            Assert.NotEmpty(dto.ReporterName);
            Assert.NotEmpty(dto.ReporterEmail);
        });
    }
}
