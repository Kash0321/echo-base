using EchoBase.Core.BlockedDocks.Queries;
using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Core.Incidences;
using EchoBase.Core.Incidences.Commands;
using EchoBase.Core.Incidences.Queries;
using EchoBase.Core.Interfaces;
using EchoBase.Core.SystemAdmin.Queries;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace EchoBase.Tests.Unit.Incidences;

public class IncidenceCommandTests
{
    // ─────────────────────────────────────────────────────────────
    // Fixtures comunes
    // ─────────────────────────────────────────────────────────────
    private static readonly Guid UserId    = Guid.NewGuid();
    private static readonly Guid ManagerId = Guid.NewGuid();
    private static readonly Guid DockId    = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IIncidenceRepository   _incidenceRepo  = Substitute.For<IIncidenceRepository>();
    private readonly IBlockedDockRepository _blockedDockRepo = Substitute.For<IBlockedDockRepository>();
    private readonly IPublisher             _publisher       = Substitute.For<IPublisher>();
    private readonly TimeProvider           _timeProvider    = Substitute.For<TimeProvider>();

    public IncidenceCommandTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
        _incidenceRepo.DockExistsAsync(DockId, Arg.Any<CancellationToken>()).Returns(true);
        _incidenceRepo.GetDockCodeAsync(DockId, Arg.Any<CancellationToken>()).Returns("NA-01");
        _blockedDockRepo.UserHasRoleAsync(ManagerId, "Manager", Arg.Any<CancellationToken>()).Returns(true);
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-01  ReportIncidence: happy path
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task ReportIncidence_ValidRequest_ReturnsSuccessWithNewId()
    {
        var handler = new ReportIncidenceHandler(_incidenceRepo, _publisher, _timeProvider);
        var command = new ReportIncidenceCommand(UserId, DockId, "Monitor roto");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _incidenceRepo.Received(1).AddAsync(Arg.Any<IncidenceReport>(), Arg.Any<CancellationToken>());
        await _incidenceRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-02  ReportIncidence: descripción vacía → error
    // ─────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReportIncidence_EmptyDescription_ReturnsDescriptionRequiredError(string description)
    {
        var handler = new ReportIncidenceHandler(_incidenceRepo, _publisher, _timeProvider);
        var command = new ReportIncidenceCommand(UserId, DockId, description);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.DescriptionRequired, result.Error);
        await _incidenceRepo.DidNotReceive().AddAsync(Arg.Any<IncidenceReport>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-03  ReportIncidence: puesto no encontrado → error
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task ReportIncidence_UnknownDock_ReturnsDockNotFoundError()
    {
        var unknownDockId = Guid.NewGuid();
        _incidenceRepo.DockExistsAsync(unknownDockId, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new ReportIncidenceHandler(_incidenceRepo, _publisher, _timeProvider);
        var command = new ReportIncidenceCommand(UserId, unknownDockId, "Silla rota");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.DockNotFound, result.Error);
        await _incidenceRepo.DidNotReceive().AddAsync(Arg.Any<IncidenceReport>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-04  ReportIncidence: se publica la notificación
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task ReportIncidence_ValidRequest_PublishesIncidenceReportedNotification()
    {
        var handler = new ReportIncidenceHandler(_incidenceRepo, _publisher, _timeProvider);
        var command = new ReportIncidenceCommand(UserId, DockId, "Teclado sin teclas");

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<EchoBase.Core.Incidences.Notifications.IncidenceReportedNotification>(n =>
                n.DockCode == "NA-01" && n.ReportedByUserId == UserId),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-05  UpdateIncidenceStatus: happy path
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateIncidenceStatus_ManagerAndExistingIncidence_ReturnsSuccess()
    {
        var incidence = MakeIncidence();
        _incidenceRepo.GetByIdAsync(incidence.Id, Arg.Any<CancellationToken>()).Returns(incidence);
        _incidenceRepo.GetDockCodeAsync(DockId, Arg.Any<CancellationToken>()).Returns("NA-01");

        var handler = new UpdateIncidenceStatusHandler(_blockedDockRepo, _incidenceRepo, _publisher, _timeProvider);
        var command = new UpdateIncidenceStatusCommand(ManagerId, incidence.Id, IncidenceStatus.UnderReview, "Revisando el problema");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(IncidenceStatus.UnderReview, incidence.Status);
        Assert.Equal(ManagerId, incidence.UpdatedByUserId);
        Assert.Equal("Revisando el problema", incidence.ManagerComment);
        await _incidenceRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-06  UpdateIncidenceStatus: rol incorrecto → error
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateIncidenceStatus_NonManager_ReturnsNotManagerError()
    {
        var nonManagerId = Guid.NewGuid();
        _blockedDockRepo.UserHasRoleAsync(nonManagerId, "Manager", Arg.Any<CancellationToken>()).Returns(false);

        var incidence = MakeIncidence();
        _incidenceRepo.GetByIdAsync(incidence.Id, Arg.Any<CancellationToken>()).Returns(incidence);

        var handler = new UpdateIncidenceStatusHandler(_blockedDockRepo, _incidenceRepo, _publisher, _timeProvider);
        var command = new UpdateIncidenceStatusCommand(nonManagerId, incidence.Id, IncidenceStatus.Resolved, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.NotManager, result.Error);
        await _incidenceRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-07  UpdateIncidenceStatus: incidencia no encontrada
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateIncidenceStatus_IncidenceNotFound_ReturnsError()
    {
        var unknownId = Guid.NewGuid();
        _incidenceRepo.GetByIdAsync(unknownId, Arg.Any<CancellationToken>()).ReturnsNull();

        var handler = new UpdateIncidenceStatusHandler(_blockedDockRepo, _incidenceRepo, _publisher, _timeProvider);
        var command = new UpdateIncidenceStatusCommand(ManagerId, unknownId, IncidenceStatus.Resolved, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.IncidenceNotFound, result.Error);
        await _incidenceRepo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-08  UpdateIncidenceStatus: se publica notificación
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateIncidenceStatus_ValidUpdate_PublishesStatusUpdatedNotification()
    {
        var incidence = MakeIncidence();
        _incidenceRepo.GetByIdAsync(incidence.Id, Arg.Any<CancellationToken>()).Returns(incidence);
        _incidenceRepo.GetDockCodeAsync(DockId, Arg.Any<CancellationToken>()).Returns("NA-01");

        var handler = new UpdateIncidenceStatusHandler(_blockedDockRepo, _incidenceRepo, _publisher, _timeProvider);
        var command = new UpdateIncidenceStatusCommand(ManagerId, incidence.Id, IncidenceStatus.Resolved, "Reparado");

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<EchoBase.Core.Incidences.Notifications.IncidenceStatusUpdatedNotification>(n =>
                n.NewStatus == IncidenceStatus.Resolved &&
                n.ManagerComment == "Reparado" &&
                n.ReportedByUserId == UserId),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-09  GetAllIncidences: usuario no Manager → error
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetAllIncidences_NonManagerUser_ReturnsNotManagerError()
    {
        var regularUserId = Guid.NewGuid();
        _blockedDockRepo.UserHasRoleAsync(regularUserId, "Manager", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GetAllIncidencesHandler(_blockedDockRepo, _incidenceRepo);
        var query   = new GetAllIncidencesQuery(regularUserId, 1, 20);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IncidenceErrors.NotManager, result.Error);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: crea una incidencia de prueba ya persistida
    // ─────────────────────────────────────────────────────────────
    // UT-IR-10  GetDockIncidences: devuelve lista paginada
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockIncidences_ActiveOnlyFalse_DelegatesToRepositoryWithNullFilter()
    {
        _incidenceRepo.GetDockIncidencesAsync(
            DockId, null, 1, 5, Arg.Any<CancellationToken>())
            .Returns((new List<IncidenceReport> { MakeIncidence() }, 1));

        var handler = new GetDockIncidencesHandler(_incidenceRepo);
        var query = new GetDockIncidencesQuery(DockId, ActiveOnly: false, Page: 1, PageSize: 5);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        await _incidenceRepo.Received(1).GetDockIncidencesAsync(
            DockId, null, 1, 5, Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-11  GetDockIncidences: filtro activas pasa los estados correctos
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockIncidences_ActiveOnlyTrue_DelegatesToRepositoryWithActiveStatuses()
    {
        _incidenceRepo.GetDockIncidencesAsync(
            DockId,
            Arg.Is<IncidenceStatus[]?>(s => s != null && s.Contains(IncidenceStatus.Open) && s.Contains(IncidenceStatus.UnderReview) && s.Length == 2),
            1, 5, Arg.Any<CancellationToken>())
            .Returns((new List<IncidenceReport>(), 0));

        var handler = new GetDockIncidencesHandler(_incidenceRepo);
        var query = new GetDockIncidencesQuery(DockId, ActiveOnly: true, Page: 1, PageSize: 5);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    // ─────────────────────────────────────────────────────────────
    // UT-IR-12  GetDockIncidences: mapea correctamente el DTO
    // ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task GetDockIncidences_WithResults_MapsDtoFieldsCorrectly()
    {
        var incidence = MakeIncidence();

        _incidenceRepo.GetDockIncidencesAsync(
            DockId, null, 1, 5, Arg.Any<CancellationToken>())
            .Returns((new List<IncidenceReport> { incidence }, 1));

        var handler = new GetDockIncidencesHandler(_incidenceRepo);
        var query = new GetDockIncidencesQuery(DockId, ActiveOnly: false, Page: 1, PageSize: 5);

        var result = await handler.Handle(query, CancellationToken.None);

        var dto = Assert.Single(result.Items);
        // Dock nav. property no cargado → valores por defecto del mapper
        Assert.Equal(string.Empty, dto.DockCode);
        Assert.Equal(string.Empty, dto.DockLocation);
        Assert.Equal("Monitor parpadeante", dto.Description);
        Assert.Equal(IncidenceStatus.Open, dto.Status);
        Assert.Equal(1, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(1, result.TotalCount);
    }

    // ─────────────────────────────────────────────────────────────
    // Helper: crea una incidencia de prueba ya persistida
    // ─────────────────────────────────────────────────────────────
    private static IncidenceReport MakeIncidence() =>
        new(Guid.CreateVersion7(), DockId, UserId, Now)
        {
            Description = "Monitor parpadeante"
        };
}
