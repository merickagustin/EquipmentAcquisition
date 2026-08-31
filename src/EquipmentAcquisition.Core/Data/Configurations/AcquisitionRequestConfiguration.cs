using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class AcquisitionRequestConfiguration : IEntityTypeConfiguration<AcquisitionRequest>
{
    public void Configure(EntityTypeBuilder<AcquisitionRequest> builder)
    {
        builder.Property(x => x.ItemDescription).IsRequired();
        builder.Property(x => x.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RequestDate).HasColumnType("datetime").IsRequired();
        builder.Property(x => x.ApprovedDate).HasColumnType("datetime");
        builder.Property(x => x.RejectedDate).HasColumnType("datetime");

        builder.Ignore(x => x.Status);

        builder.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EquipmentCategory)
            .WithMany()
            .HasForeignKey(x => x.EquipmentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.RequestedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DepartmentId, x.EquipmentCategoryId, x.RequestDate });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AcquisitionRequest_MutuallyExclusiveDates",
            "[ApprovedDate] IS NULL OR [RejectedDate] IS NULL"));
    }
}
