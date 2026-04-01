using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="BlockedDock"/> para EF Core.
/// Define la tabla, índices para consultas de solapamiento y disponibilidad,
/// y las relaciones FK hacia <see cref="Dock"/> y <see cref="User"/>.
/// </summary>
internal sealed class BlockedDockConfiguration : IEntityTypeConfiguration<BlockedDock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedDock> builder)
    {
        builder.ToTable("BlockedDocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(b => b.StartDate)
            .IsRequired();

        builder.Property(b => b.EndDate)
            .IsRequired();

        builder.Property(b => b.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.IsActive)
            .IsRequired();

        // Índice para consultas de solapamiento: dock + rango de fechas + activo
        builder.HasIndex(b => new { b.DockId, b.StartDate, b.EndDate })
            .HasFilter("[IsActive] = 1");

        // Índice para consultas de disponibilidad rápida por puesto
        builder.HasIndex(b => new { b.DockId, b.IsActive });

        builder.HasOne(b => b.Dock)
            .WithMany()
            .HasForeignKey(b => b.DockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BlockedByUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
