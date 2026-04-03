using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="DockZone"/> para EF Core.
/// Define la tabla, restricciones de columnas, índices y la relación 1:N con <see cref="DockTable"/>.
/// Los puestos de trabajo se acceden a través de la jerarquía DockZone → DockTable → Dock.
/// </summary>
internal sealed class DockZoneConfiguration : IEntityTypeConfiguration<DockZone>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DockZone> builder)
    {
        builder.ToTable("DockZones");

        builder.HasKey(dz => dz.Id);

        builder.Property(dz => dz.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(dz => dz.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(dz => dz.Name)
            .IsUnique();

        builder.Property(dz => dz.Description)
            .HasMaxLength(500);

        builder.Property(dz => dz.Orientation)
            .HasConversion<int>()
            .HasDefaultValue(ZoneOrientation.Horizontal);

        builder.Property(dz => dz.Order)
            .HasDefaultValue(0);

        builder.HasMany(dz => dz.Tables)
            .WithOne(t => t.DockZone)
            .HasForeignKey(t => t.DockZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
