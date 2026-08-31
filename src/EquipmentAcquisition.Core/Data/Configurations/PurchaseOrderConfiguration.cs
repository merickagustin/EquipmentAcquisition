using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.Property(x => x.PoNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.PoNumber).IsUnique();
        builder.HasIndex(x => x.AcquisitionRequestId).IsUnique();

        builder.Property(x => x.OrderDate).HasColumnType("datetime");

        builder.HasOne(x => x.AcquisitionRequest)
            .WithOne(x => x.PurchaseOrder)
            .HasForeignKey<PurchaseOrder>(x => x.AcquisitionRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
