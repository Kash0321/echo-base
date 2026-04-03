using EchoBase.Core.Entities;
using EchoBase.Core.Entities.Enums;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="Dock"/> para EF Core.
/// Define la tabla, restricciones de columnas, índices (incluido el de <c>DockTableId</c>)
/// y la relación 1:N con <see cref="Reservation"/>.
/// </summary>
internal sealed class DockConfiguration : IEntityTypeConfiguration<Dock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Dock> builder)
    {
        builder.ToTable("Docks");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.Property(d => d.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Equipment)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Side)
            .HasConversion<int>()
            .HasDefaultValue(DockSide.A);

        builder.HasIndex(d => d.DockTableId);

        builder.HasMany(d => d.Reservations)
            .WithOne(r => r.Dock)
            .HasForeignKey(r => r.DockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
