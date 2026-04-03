using EchoBase.Core.Interfaces;
using EchoBase.Infrastructure;
using EchoBase.Infrastructure.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBase.Tests.Unit.Notifications;

/// <summary>
/// Tests unitarios que verifican el comportamiento del feature flag
/// <c>Features:TeamsNotificationsEnabled</c> para las notificaciones de Microsoft Teams.
/// </summary>
public class TeamsFeatureFlagTests
{
    // ──────────────────────────────────────────────────────────────
    // NullTeamsNotificationService — comportamiento no operativo
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task NullTeamsNotificationService_SendChatMessageAsync_CompletesWithoutException()
    {
        var sut = new NullTeamsNotificationService();

        // No debe lanzar ninguna excepción
        await sut.SendChatMessageAsync("user-id", "mensaje de prueba");
    }

    [Fact]
    public async Task NullTeamsNotificationService_SendChatMessageAsync_ReturnsCompletedTask()
    {
        var sut = new NullTeamsNotificationService();

        var task = sut.SendChatMessageAsync("user-id", "mensaje de prueba");
        await task;

        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NullTeamsNotificationService_MultipleCallsWithDifferentArguments_AllComplete()
    {
        var sut = new NullTeamsNotificationService();

        await sut.SendChatMessageAsync("user-1", "mensaje 1");
        await sut.SendChatMessageAsync("user-2", "<b>HTML</b> con caracteres especiales &amp;");
        await sut.SendChatMessageAsync(string.Empty, string.Empty, CancellationToken.None);
    }

    // ──────────────────────────────────────────────────────────────
    // Registro en DI según valor del feature flag
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddEchoBaseNotifications_WhenTeamsDisabled_RegistersNullTeamsService()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(teamsEnabled: false, useStubs: false);

        services.AddEchoBaseNotifications(config);

        AssertTeamsImplementation<NullTeamsNotificationService>(services);
    }

    [Fact]
    public void AddEchoBaseNotifications_WhenTeamsDisabledAndStubsEnabled_StillRegistersNullTeamsService()
    {
        // El flag de Teams tiene prioridad sobre UseDevelopmentStubs para esa funcionalidad
        var services = new ServiceCollection();
        var config = BuildConfig(teamsEnabled: false, useStubs: true);

        services.AddEchoBaseNotifications(config);

        AssertTeamsImplementation<NullTeamsNotificationService>(services);
    }

    [Fact]
    public void AddEchoBaseNotifications_WhenTeamsEnabledAndStubsEnabled_RegistersLogTeamsService()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(teamsEnabled: true, useStubs: true);

        services.AddEchoBaseNotifications(config);

        AssertTeamsImplementation<LogTeamsNotificationService>(services);
    }

    [Fact]
    public void AddEchoBaseNotifications_WhenTeamsEnabledAndStubsDisabled_RegistersGraphTeamsService()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(teamsEnabled: true, useStubs: false);

        services.AddEchoBaseNotifications(config);

        AssertTeamsImplementation<GraphTeamsNotificationService>(services);
    }

    [Fact]
    public void AddEchoBaseNotifications_WhenFlagAbsent_DefaultsToTeamsEnabled()
    {
        // Sin la sección Features, el flag debe ser true por defecto
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notifications:UseDevelopmentStubs"] = "true"
            })
            .Build();

        services.AddEchoBaseNotifications(config);

        // Con stubs activos y flag ausente (= true por defecto), debe registrar LogTeamsNotificationService
        AssertTeamsImplementation<LogTeamsNotificationService>(services);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static IConfiguration BuildConfig(bool teamsEnabled, bool useStubs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:TeamsNotificationsEnabled"] = teamsEnabled.ToString().ToLower(),
                ["Notifications:UseDevelopmentStubs"] = useStubs.ToString().ToLower()
            })
            .Build();

    private static void AssertTeamsImplementation<TExpected>(IServiceCollection services)
        where TExpected : ITeamsNotificationService
    {
        var descriptor = services.SingleOrDefault(
            sd => sd.ServiceType == typeof(ITeamsNotificationService));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TExpected), descriptor.ImplementationType);
    }
}
