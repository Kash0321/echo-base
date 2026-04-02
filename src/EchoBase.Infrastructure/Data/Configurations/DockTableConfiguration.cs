using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="DockTable"/> para EF Core.
/// Define la tabla, restricciones de columnas e índice único por zona y clave de mesa.
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
    }
}
