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

    // ── IT-IR-10  GetDockIncidences: devuelve solo las del puesto indicado ────
    [Fact]
    public async Task GetDockIncidences_ReturnsOnlyIncidencesForSpecifiedDock()
    {
        await Mediator.Send(new ReportIncidenceCommand(TestUserId,    DockNA01, "Monitor roto"));
        await Mediator.Send(new ReportIncidenceCommand(AnotherUserId, DockNA01, "Ratón sin cable"));
        await Mediator.Send(new ReportIncidenceCommand(TestUserId,    DockNA02, "Silla rota"));

        var query  = new GetDockIncidencesQuery(DockNA01, ActiveOnly: false, Page: 1, PageSize: 5);
        var result = await Mediator.Send(query);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, dto => Assert.Equal("N-A01", dto.DockCode));
    }

    // ── IT-IR-11  GetDockIncidences: filtro activas excluye resueltas y rechazadas ─
    [Fact]
    public async Task GetDockIncidences_ActiveOnly_ExcludesResolvedAndRejected()
    {
        var r1 = await Mediator.Send(new ReportIncidenceCommand(TestUserId, DockNA01, "Abierta"));
        var r2 = await Mediator.Send(new ReportIncidenceCommand(TestUserId, DockNA01, "En revisión"));
        var r3 = await Mediator.Send(new ReportIncidenceCommand(TestUserId, DockNA01, "Resuelta"));
        var r4 = await Mediator.Send(new ReportIncidenceCommand(TestUserId, DockNA01, "Rechazada"));

        Assert.True(r1.IsSuccess); Assert.True(r2.IsSuccess); Assert.True(r3.IsSuccess); Assert.True(r4.IsSuccess);

        await Mediator.Send(new UpdateIncidenceStatusCommand(ManagerUserId, r2.Value, IncidenceStatus.UnderReview, null));
        await Mediator.Send(new UpdateIncidenceStatusCommand(ManagerUserId, r3.Value, IncidenceStatus.Resolved, "Arreglado"));
        await Mediator.Send(new UpdateIncidenceStatusCommand(ManagerUserId, r4.Value, IncidenceStatus.Rejected, "Duplicada"));

        var query  = new GetDockIncidencesQuery(DockNA01, ActiveOnly: true, Page: 1, PageSize: 5);
        var result = await Mediator.Send(query);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, dto =>
            Assert.True(dto.Status == IncidenceStatus.Open || dto.Status == IncidenceStatus.UnderReview));
    }

    // ── IT-IR-12  GetDockIncidences: paginación funciona correctamente ────────
    [Fact]
    public async Task GetDockIncidences_Pagination_ReturnsCorrectPage()
    {
        for (var i = 1; i <= 7; i++)
            await Mediator.Send(new ReportIncidenceCommand(TestUserId, DockNA01, $"Incidencia {i}"));

        var page1 = await Mediator.Send(new GetDockIncidencesQuery(DockNA01, ActiveOnly: false, Page: 1, PageSize: 5));
        var page2 = await Mediator.Send(new GetDockIncidencesQuery(DockNA01, ActiveOnly: false, Page: 2, PageSize: 5));

        Assert.Equal(7, page1.TotalCount);
        Assert.Equal(5, page1.Items.Count);
        Assert.Equal(7, page2.TotalCount);
        Assert.Equal(2, page2.Items.Count);
    }

    // ── IT-IR-13  GetDockIncidences: puesto sin incidencias → lista vacía ────
    [Fact]
    public async Task GetDockIncidences_NoDockIncidences_ReturnsEmptyResult()
    {
        var query  = new GetDockIncidencesQuery(DockNA01, ActiveOnly: false, Page: 1, PageSize: 5);
        var result = await Mediator.Send(query);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
