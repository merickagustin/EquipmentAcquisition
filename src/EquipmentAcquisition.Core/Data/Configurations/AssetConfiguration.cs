using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(x => x.AssetTag).HasMaxLength(30).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.AcquiredDate).HasColumnType("datetime");
        builder.Property(x => x.LastUpdated).HasColumnType("datetime");

        builder.HasIndex(x => x.AssetTag).IsUnique();
        builder.HasIndex(x => new { x.DepartmentId, x.Status });

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(x => x.Assets)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
