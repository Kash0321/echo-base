using EchoBase.Core.Interfaces;
using EchoBase.Core.Reservations.Commands;
using EchoBase.Infrastructure.Data;
using EchoBase.Infrastructure.Email;
using EchoBase.Infrastructure.Notifications;
using EchoBase.Infrastructure.Repositories;
using EchoBase.Infrastructure.Teams;
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
        });

        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IBlockedDockRepository, BlockedDockRepository>();
        services.AddScoped<IDockMapRepository, DockMapRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Registra los servicios de notificación (email SMTP y Teams vía Graph).
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración de la aplicación para leer secciones SMTP y MicrosoftGraph.</param>
    /// <returns>La misma <paramref name="services"/> para encadenamiento fluido.</returns>
    public static IServiceCollection AddEchoBaseNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.Configure<GraphSettings>(configuration.GetSection(GraphSettings.SectionName));
        services.AddScoped<ITeamsNotificationService, GraphTeamsNotificationService>();

        return services;
    }
}
