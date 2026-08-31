using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CacheEntity = EquipmentAcquisition.Domain.Entities.EquipmentAcquisitionDetailCache;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class EquipmentAcquisitionDetailCacheConfiguration : IEntityTypeConfiguration<CacheEntity>
{
    public void Configure(EntityTypeBuilder<CacheEntity> builder)
    {
        builder.HasKey(x => x.AcquisitionRequestId);

        // Value copy, not an identity column — the refresh path assigns it explicitly.
        builder.Property(x => x.AcquisitionRequestId).ValueGeneratedNever();

        builder.Property(x => x.DepartmentCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.DepartmentName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EquipmentCategoryName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RequestedByName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.RequestedByJobTitle).HasMaxLength(100);
        builder.Property(x => x.ApprovedByName).HasMaxLength(150);
        builder.Property(x => x.ItemDescription).IsRequired();
        builder.Property(x => x.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PoNumber).HasMaxLength(50);
        builder.Property(x => x.VendorName).HasMaxLength(150);
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RequestDate).HasColumnType("datetime");
        builder.Property(x => x.ApprovedDate).HasColumnType("datetime");
        builder.Property(x => x.RejectedDate).HasColumnType("datetime");
        builder.Property(x => x.OrderDate).HasColumnType("datetime");
        builder.Property(x => x.RefreshedAt).HasColumnType("datetime");

        // No foreign keys in either direction — deliberately standalone, see table-design.md.
        builder.HasIndex(x => new { x.DepartmentId, x.Status, x.RequestDate });
        builder.HasIndex(x => x.EquipmentCategoryId);
        builder.HasIndex(x => x.VendorId);
        builder.HasIndex(x => x.RequestedByEmployeeId);
        builder.HasIndex(x => x.ApprovedByEmployeeId);
    }
}
