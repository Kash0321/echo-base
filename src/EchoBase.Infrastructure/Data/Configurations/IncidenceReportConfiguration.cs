using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="IncidenceReport"/> para EF Core.
/// Define la tabla, conversiones de enum a entero, longitudes máximas y FKs hacia Dock y User.
/// </summary>
internal sealed class IncidenceReportConfiguration : IEntityTypeConfiguration<IncidenceReport>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<IncidenceReport> builder)
    {
        builder.ToTable("IncidenceReports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.ManagerComment)
            .HasMaxLength(2000);

        // Índice para consultas por usuario (ver mis incidencias)
        builder.HasIndex(r => r.ReportedByUserId);

        // Índice para filtrar por puesto
        builder.HasIndex(r => r.DockId);

        builder.HasOne(r => r.Dock)
            .WithMany()
            .HasForeignKey(r => r.DockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
