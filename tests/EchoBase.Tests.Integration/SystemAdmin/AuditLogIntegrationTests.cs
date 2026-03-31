using EchoBase.Core.Entities.Enums;
using EchoBase.Core.SystemAdmin.Commands;
using EchoBase.Core.SystemAdmin.Queries;
using EchoBase.Tests.Integration.Infrastructure;

namespace EchoBase.Tests.Integration.SystemAdmin;

/// <summary>
/// Tests de integración para el log de auditoría:
/// escritura automática via <see cref="AuditLoggingBehavior{TRequest,TResponse}"/>
/// y consulta paginada via <see cref="GetAuditLogsQuery"/>.
/// </summary>
public sealed class AuditLogIntegrationTests : IntegrationTestBase
{
    // ── IT-AL-01 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_SuccessfulCommand_IsPersistedAutomatically()
    {
        // Arrange + Act
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Test"));

        // Assert via query
        var page = await Mediator.Send(new GetAuditLogsQuery(null, null, null, null, 1, 20));
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(AuditAction.MaintenanceModeChanged, page.Items[0].Action);
    }

    // ── IT-AL-02 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_FailedCommand_IsNotPersisted()
    {
        // Arrange: send command that will fail (non-admin user)
        await Mediator.Send(new SetMaintenanceModeCommand(TestUserId, true, "Unauthorized"));

        // Assert
        var page = await Mediator.Send(new GetAuditLogsQuery(null, null, null, null, 1, 20));
        Assert.Equal(0, page.TotalCount);
    }

    // ── IT-AL-03 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_GetPagedWithActionFilter_ReturnsOnlyMatchingAction()
    {
        // Arrange: create two different actions
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Act1"));
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, false, null));

        // Act: filter by MaintenanceModeChanged
        var page = await Mediator.Send(new GetAuditLogsQuery(
            null, null, AuditAction.MaintenanceModeChanged, null, 1, 20));

        // Assert
        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, item => Assert.Equal(AuditAction.MaintenanceModeChanged, item.Action));
    }

    // ── IT-AL-04 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_Pagination_ReturnsCorrectPage()
    {
        // Arrange: 3 entries
        for (int i = 0; i < 3; i++)
            await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, i % 2 == 0, i % 2 == 0 ? $"Act{i}" : null));

        // Act: page size 2, page 1
        var page1 = await Mediator.Send(new GetAuditLogsQuery(null, null, null, null, 1, 2));
        var page2 = await Mediator.Send(new GetAuditLogsQuery(null, null, null, null, 2, 2));

        // Assert
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.Equal(2, page1.TotalPages);
    }

    // ── IT-AL-05 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_GetPaged_ReturnsPerformedByName()
    {
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Prueba nombre"));

        var page = await Mediator.Send(new GetAuditLogsQuery(null, null, null, null, 1, 20));

        Assert.Single(page.Items);
        Assert.Equal("Admin User", page.Items[0].PerformedByName);
    }

    // ── IT-AL-06 ──────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_GetPagedWithUserSearch_FiltersResults()
    {
        await Mediator.Send(new SetMaintenanceModeCommand(AdminUserId, true, "Filtro usuario"));

        // Search by known name
        var match = await Mediator.Send(new GetAuditLogsQuery(null, null, null, "Admin User", 1, 20));
        var noMatch = await Mediator.Send(new GetAuditLogsQuery(null, null, null, "Inexistente", 1, 20));

        Assert.Equal(1, match.TotalCount);
        Assert.Equal(0, noMatch.TotalCount);
    }
}
