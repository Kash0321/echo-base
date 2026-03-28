using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
}
