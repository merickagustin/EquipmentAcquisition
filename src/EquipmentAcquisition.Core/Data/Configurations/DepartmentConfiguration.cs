using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquipmentAcquisition.Core.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
