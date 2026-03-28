using EchoBase.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EchoBase.Infrastructure.Data.Configurations;

internal sealed class DockZoneConfiguration : IEntityTypeConfiguration<DockZone>
{
    public void Configure(EntityTypeBuilder<DockZone> builder)
    {
        builder.ToTable("DockZones");

        builder.HasKey(dz => dz.Id);

        builder.Property(dz => dz.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(dz => dz.Name)
            .IsUnique();

        builder.Property(dz => dz.Description)
            .HasMaxLength(500);

        builder.HasMany(dz => dz.Docks)
            .WithOne(d => d.DockZone)
            .HasForeignKey(d => d.DockZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
