using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class CacheRefreshQueueConfiguration : IEntityTypeConfiguration<CacheRefreshQueue>
{
    public void Configure(EntityTypeBuilder<CacheRefreshQueue> builder)
    {
        builder.Property(x => x.EnqueuedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
