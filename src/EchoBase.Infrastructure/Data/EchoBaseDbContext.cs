using System.Reflection;
using EchoBase.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoBase.Infrastructure.Data;

/// <summary>
/// Contexto principal de Entity Framework Core para EchoBase.
/// Registra todas las entidades del dominio y aplica las configuraciones Fluent API
/// definidas en el ensamblado de infraestructura.
/// </summary>
public sealed class EchoBaseDbContext(DbContextOptions<EchoBaseDbContext> options)
    : DbContext(options)
{
    /// <summary>Usuarios del sistema integrados con Azure AD.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Roles de autorización (BasicUser, Manager).</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Puestos de trabajo físicos.</summary>
    public DbSet<Dock> Docks => Set<Dock>();

    /// <summary>Zonas físicas que agrupan puestos (Nostromo, Derelict).</summary>
    public DbSet<DockZone> DockZones => Set<DockZone>();

    /// <summary>Reservas de puestos realizadas por los usuarios.</summary>
    public DbSet<Reservation> Reservations => Set<Reservation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
