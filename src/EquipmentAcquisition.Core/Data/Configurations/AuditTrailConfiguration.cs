using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.Property(x => x.TableAffected).HasMaxLength(40).IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DateApplied)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(x => new { x.TableAffected, x.AffectedId, x.DateApplied });

        builder.HasOne(x => x.ChangedByEmployee)
            .WithMany()
            .HasForeignKey(x => x.ChangedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AuditTrail_Action",
            "[Action] IN ('Insert', 'Update', 'Delete')"));
    }
}
