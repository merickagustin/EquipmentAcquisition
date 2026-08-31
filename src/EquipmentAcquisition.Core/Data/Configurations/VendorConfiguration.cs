using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(255);
    }
}
