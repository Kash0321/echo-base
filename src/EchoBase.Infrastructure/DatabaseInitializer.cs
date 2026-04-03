using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EchoBase.Infrastructure;

/// <summary>
/// Métodos de extensión para inicializar la base de datos desde el host de la aplicación.
/// Encapsula las dependencias de EF Core para que la capa de presentación no las referencie directamente.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Aplica las migraciones pendientes y ejecuta el seeder de datos maestros.
    /// Debe llamarse durante el arranque de la aplicación, después de construir el host.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios del host.</param>
    /// <param name="isDevelopment">
    /// <see langword="true"/> para inicializar también el usuario de desarrollo con rol Manager.
    /// Solo debe usarse en entorno de desarrollo local.
    /// </param>
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool isDevelopment = false)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EchoBaseDbContext>();

        await context.Database.MigrateAsync();
        await DbSeeder.SeedAsync(context);
        await DbSeeder.SeedRolesAsync(context);

        if (isDevelopment)
            await DbSeeder.SeedDevUserAsync(context);
    }
}
