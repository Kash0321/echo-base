using EchoBase.Core.Entities;
using EchoBase.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

/// <summary>
/// Configuración Fluent API de <see cref="Reservation"/> para EF Core.
/// Define la tabla, conversiones de enums a entero, índices compuestos para consultas
/// de disponibilidad y límite diario de usuario, y las FKs hacia <see cref="User"/> y <see cref="Dock"/>.
/// </summary>
/// <remarks>
/// La unicidad semántica de franjas horarias (<see cref="EchoBase.Core.Entities.Enums.TimeSlot.Both"/>
/// solapa con <c>Morning</c> y <c>Afternoon</c>) se impone a nivel de dominio, no mediante un índice único en BD.
/// </remarks>
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd()
            .HasValueGenerator<UuidV7ValueGenerator>();

        builder.Property(r => r.Date)
            .IsRequired();

        builder.Property(r => r.TimeSlot)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        // Índice para consultas de disponibilidad por puesto y fecha
        builder.HasIndex(r => new { r.DockId, r.Date });

        // Índice para validar el límite de reservas diarias por usuario
        builder.HasIndex(r => new { r.UserId, r.Date });

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reservations)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Dock)
            .WithMany(d => d.Reservations)
            .HasForeignKey(r => r.DockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
