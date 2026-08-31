using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.Property(x => x.Label).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Route).HasMaxLength(200);

        builder.HasIndex(x => new { x.ParentId, x.DisplayOrder });

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
