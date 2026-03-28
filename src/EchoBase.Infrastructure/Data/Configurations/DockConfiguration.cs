using EchoBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

internal sealed class DockConfiguration : IEntityTypeConfiguration<Dock>
{
    public void Configure(EntityTypeBuilder<Dock> builder)
    {
        builder.ToTable("Docks");

        builder.HasKey(d => d.Id);

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

        builder.HasIndex(d => d.DockZoneId);

        builder.HasMany(d => d.Reservations)
            .WithOne(r => r.Dock)
            .HasForeignKey(r => r.DockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
