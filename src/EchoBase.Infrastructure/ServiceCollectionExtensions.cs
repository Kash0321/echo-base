using EchoBase.Core.Common;
using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Infrastructure.Data;
using EchoBase.Infrastructure.Email;
using EchoBase.Infrastructure.Notifications;
using EchoBase.Infrastructure.Repositories;
using EchoBase.Infrastructure.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBase.Infrastructure;

/// <summary>
/// Métodos de extensión para registrar los servicios de infraestructura
/// en el contenedor de inyección de dependencias.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra <see cref="EchoBaseDbContext"/> con el proveedor de base de datos indicado.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="connectionString">Cadena de conexión a la base de datos.</param>
    /// <param name="useSqlite">
    /// <see langword="true"/> para usar SQLite (desarrollo local);
    /// <see langword="false"/> para usar Azure SQL / SQL Server (producción).
    /// Por defecto: <see langword="true"/>.
    /// </param>
    /// <returns>La misma <paramref name="services"/> para encadenamiento fluido.</returns>
    /// <remarks>
    /// Para migrar a Azure SQL en producción basta con pasar <c>useSqlite: false</c>
    /// y apuntar <paramref name="connectionString"/> al connection string de Azure SQL.
    /// No es necesario tocar el <see cref="EchoBaseDbContext"/> ni las configuraciones.
    /// </remarks>
    public static IServiceCollection AddEchoBaseDatabase(
        this IServiceCollection services,
        string connectionString,
        bool useSqlite = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EchoBaseDbContext>(options =>
        {
            if (useSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Registra MediatR, repositorios y servicios transversales de la aplicación.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <returns>La misma <paramref name="services"/> para encadenamiento fluido.</returns>
    public static IServiceCollection AddEchoBaseServices(
        this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateReservationCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ReservationCreatedEmailHandler).Assembly);
            cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));
        });

        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IBlockedDockRepository, BlockedDockRepository>();
        services.AddScoped<IDockMapRepository, DockMapRepository>();
        services.AddScoped<IDockAdminRepository, DockAdminRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
        services.AddScoped<IIncidenceRepository, IncidenceRepository>();
        services.AddSingleton(TimeProvider.System);

        services.AddHostedService<ReservationReminderService>();

        return services;
    }

    /// <summary>
    /// Registra los servicios de notificación (email SMTP y Teams vía Graph).
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración de la aplicación para leer secciones SMTP, MicrosoftGraph y Features.</param>
    /// <returns>La misma <paramref name="services"/> para encadenamiento fluido.</returns>
    /// <remarks>
    /// El feature flag <c>Features:TeamsNotificationsEnabled</c> controla globalmente las notificaciones
    /// de Teams. Cuando es <see langword="false"/>, se registra <see cref="NullTeamsNotificationService"/>
    /// y no se envía ningún mensaje por Teams, independientemente de las preferencias de usuario.
    /// Por defecto es <see langword="true"/> para mantener compatibilidad hacia atrás.
    /// </remarks>
    public static IServiceCollection AddEchoBaseNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useStubs = configuration.GetValue("Notifications:UseDevelopmentStubs", false);
        var teamsEnabled = configuration.GetValue("Features:TeamsNotificationsEnabled", true);

        // ── Email ──────────────────────────────────────────────────────────
        if (useStubs)
            services.AddScoped<IEmailService, LogEmailService>();
        else
        {
            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        // ── Teams ──────────────────────────────────────────────────────────
        if (!teamsEnabled)
            services.AddScoped<ITeamsNotificationService, NullTeamsNotificationService>();
        else if (useStubs)
            services.AddScoped<ITeamsNotificationService, LogTeamsNotificationService>();
        else
        {
            services.Configure<GraphSettings>(configuration.GetSection(GraphSettings.SectionName));
            services.AddScoped<ITeamsNotificationService, GraphTeamsNotificationService>();
        }

        return services;
    }
}
