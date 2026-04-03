using EchoBase.Core.Common;
using EchoBase.Core.Entities;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Infrastructure;
using EchoBase.Infrastructure.Data;
using EchoBase.Infrastructure.Repositories;
using EchoBase.Tests.Integration.Infrastructure.Stubs;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBase.Tests.Integration.Infrastructure;

/// <summary>
/// Clase base para tests de integración de EchoBase.
/// Cada instancia (una por clase de test) obtiene una base de datos SQLite
/// en memoria propia e independiente, garantizando el aislamiento completo.
/// </summary>
/// <remarks>
/// Estrategia:
/// <list type="bullet">
///   <item>Motor: SQLite in-memory con nombre de BD único por instancia (Mode=Memory;Cache=Shared).</item>
///   <item>Esquema: creado con <c>EnsureCreated</c> (sin migraciones) para máxima velocidad.</item>
///   <item>Datos maestros: cargados via <see cref="DbSeeder"/> (zonas, puestos, roles).</item>
///   <item>Usuarios de prueba: insertados en <see cref="InitializeAsync"/>.</item>
///   <item>Tiempo: congelado al inicio del día UTC actual. Cambiar la fecha modifica el comportamiento de los handlers.</item>
///   <item>Servicios externos: IEmailService y ITeamsNotificationService sustituidos por stubs no-op.</item>
///   <item>MediatR: pipeline completo con handlers reales de Core e Infrastructure.</item>
/// </list>
/// </remarks>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    // ── GUIDs de datos maestros (coinciden con DbSeeder) ─────────────────────
    protected static readonly Guid DockNA01 = new("b0000000-0000-0000-0001-000000000001");
    protected static readonly Guid DockNA02 = new("b0000000-0000-0000-0001-000000000002");
    protected static readonly Guid DockNB01 = new("b0000000-0000-0000-0002-000000000001");

    // ── GUIDs de usuarios de prueba ───────────────────────────────────────────
    protected static readonly Guid TestUserId    = new("e0000000-0000-0000-0001-000000000001");
    protected static readonly Guid AnotherUserId = new("e0000000-0000-0000-0001-000000000002");
    protected static readonly Guid ManagerUserId = new("e0000000-0000-0000-0001-000000000003");
    protected static readonly Guid AdminUserId   = new("e0000000-0000-0000-0001-000000000004");

    // ── Infraestructura DI ────────────────────────────────────────────────────
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    // Conexión SQLite in-memory mantenida abierta para que el esquema persista
    // durante todo el ciclo de vida del test (patrón recomendado por EF Core docs)
    private SqliteConnection _connection = null!;

    protected IMediator Mediator { get; private set; } = null!;
    protected EchoBaseDbContext DbContext { get; private set; } = null!;

    /// <summary>Fecha que los handlers consideran "hoy". Corresponde al inicio del día UTC actual.</summary>
    protected DateOnly Today { get; private set; }

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        var frozenTime = FrozenTimeProvider.AtStartOfToday();
        Today = DateOnly.FromDateTime(frozenTime.GetUtcNow().DateTime);

        // Mantener la conexión SQLite in-memory abierta durante todo el test.
        // Es el patrón oficial de EF Core para SQLite in-memory: si la conexión
        // se cierra, todas las tablas desaparecen.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var services = new ServiceCollection();

        // Pasar la conexión ya abierta; EF Core la reutilizará sin cerrarla
        services.AddDbContext<EchoBaseDbContext>(o => o.UseSqlite(_connection));

        // Repositorios reales (internal — requiere InternalsVisibleTo en Infrastructure)
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IBlockedDockRepository, BlockedDockRepository>();
        services.AddScoped<IDockMapRepository, DockMapRepository>();
        services.AddScoped<IDockAdminRepository, DockAdminRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IIncidenceRepository, IncidenceRepository>();

        // MediatR: handlers de Core (negocio) + Infrastructure (notificaciones)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateReservationCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(EchoBase.Infrastructure.ServiceCollectionExtensions).Assembly);
            cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));
        });

        // Servicios externos: stubs no-op para aislar del mundo real
        services.AddScoped<IEmailService, NullEmailService>();
        services.AddScoped<ITeamsNotificationService, NullTeamsNotificationService>();

        // Tiempo congelado: el handler verá Today == fecha del test
        services.AddSingleton<TimeProvider>(frozenTime);

        // Logging: requerido por los notification handlers internos
        services.AddLogging();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();

        DbContext = _scope.ServiceProvider.GetRequiredService<EchoBaseDbContext>();
        Mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();

        // Crear esquema + datos maestros
        await DbContext.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(DbContext);
        await DbSeeder.SeedRolesAsync(DbContext);

        // Usuarios de prueba
        var systemAdminRole = await DbContext.Roles.SingleAsync(r => r.Name == "SystemAdmin");
        var managerRole = await DbContext.Roles.SingleAsync(r => r.Name == "Manager");

        var adminUser = new User(AdminUserId) { Name = "Admin User", Email = "admin@echobase.com" };
        adminUser.Roles.Add(systemAdminRole);

        var managerUser = new User(ManagerUserId) { Name = "Manager User", Email = "manager@echobase.com" };
        managerUser.Roles.Add(managerRole);

        DbContext.Users.AddRange(
            new User(TestUserId)    { Name = "Test User",    Email = "test@echobase.com"    },
            new User(AnotherUserId) { Name = "Another User", Email = "other@echobase.com"   },
            managerUser,
            adminUser
        );
        await DbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        _scope.Dispose();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
