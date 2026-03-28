using EchoBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.Id);

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
