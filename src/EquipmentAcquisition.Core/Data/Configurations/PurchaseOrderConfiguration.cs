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

        // Filtered, not a plain unique index — soft-deleted rows must not count, or a
        // replacement PO could never be created for a request whose original PO was
        // removed. PoNumber's index stays unfiltered: it's generated from the row's own
        // (never-reused) Id, so it can never collide regardless of delete state.
        builder.HasIndex(x => x.AcquisitionRequestId).IsUnique().HasFilter("[IsDeleted] = 0");

        // Soft delete — every normal query (including through AcquisitionRequest's
        // PurchaseOrder navigation) sees only active rows automatically. The one place
        // this is deliberately bypassed: VendorRepository.HasPurchaseOrdersAsync, which
        // must reflect the real FK regardless of IsDeleted — a soft-deleted PO row still
        // physically references its Vendor, and the DB's Restrict constraint doesn't care
        // about the flag.
        //
        // EF warns that Asset's required PurchaseOrder navigation could misbehave under
        // this filter (a soft-deleted PO would vanish from an Include/join even though
        // Asset.PurchaseOrderId still points at it). Not applicable today — nothing in
        // this codebase queries through Asset.PurchaseOrder; AssetRepository always uses
        // the raw PurchaseOrderId column. Worth re-checking if that ever changes.
        builder.HasQueryFilter(x => !x.IsDeleted);

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
