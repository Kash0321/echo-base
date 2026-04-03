using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="DockTable"/> para EF Core.
/// Define la tabla, restricciones de columnas, índice único por zona y clave de mesa,
/// y la relación 1:N con <see cref="Dock"/>.
/// </summary>
internal sealed class DockTableConfiguration : IEntityTypeConfiguration<DockTable>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DockTable> builder)
    {
        builder.ToTable("DockTables");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(t => t.TableKey)
            .IsRequired()
            .HasMaxLength(20);

        // Índice único: no pueden existir dos mesas con la misma clave dentro de una zona
        builder.HasIndex(t => new { t.DockZoneId, t.TableKey })
            .IsUnique();

        builder.Property(t => t.Locator)
            .HasMaxLength(100);

        builder.Property(t => t.Order)
            .HasDefaultValue(0);

        // Relación 1:N con Dock — la mesa contiene sus puestos de trabajo
        builder.HasMany(t => t.Docks)
            .WithOne(d => d.DockTable)
            .HasForeignKey(d => d.DockTableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
