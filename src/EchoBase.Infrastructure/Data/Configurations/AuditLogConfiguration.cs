using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="AuditLog"/> para EF Core.
/// Define la tabla, conversión del enum <c>AuditAction</c> y los índices
/// para las consultas de filtrado por usuario, acción y fecha.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(a => a.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Details)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Índice para filtrado por usuario, acción y timestamp (queries de auditoría)
        builder.HasIndex(a => new { a.PerformedByUserId, a.Timestamp });
        builder.HasIndex(a => new { a.Action, a.Timestamp });

        // FK opcional hacia User (puede ser null si la acción la realiza el sistema)
        builder.HasOne<Core.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.PerformedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
