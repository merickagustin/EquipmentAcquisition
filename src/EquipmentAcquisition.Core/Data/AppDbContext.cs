using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AcquisitionRequest> AcquisitionRequests => Set<AcquisitionRequest>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<EquipmentAcquisitionDetailCache> EquipmentAcquisitionDetailCaches => Set<EquipmentAcquisitionDetailCache>();
    public DbSet<CacheRefreshQueue> CacheRefreshQueue => Set<CacheRefreshQueue>();
    public DbSet<AuditTrail> AuditTrail => Set<AuditTrail>();

    // Keyless — backs FromSqlRaw against usp_GetDepartmentSpendReport, not a real table.
    public DbSet<ReportRowDto> ReportRows => Set<ReportRowDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.Entity<ReportRowDto>().HasNoKey().ToView(null)
            .Property(r => r.TotalSpend).HasColumnType("decimal(18,2)");
    }
}
