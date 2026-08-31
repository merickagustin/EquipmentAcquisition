using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _context;

    public VendorRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Vendor>> GetAllAsync() =>
        _context.Vendors.AsNoTracking().OrderBy(v => v.Name).ToListAsync();

    public Task<Vendor?> GetByIdAsync(int id) =>
        _context.Vendors.FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Vendor> AddAsync(Vendor vendor)
    {
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();
        return vendor;
    }

    public Task UpdateAsync(Vendor vendor) => _context.SaveChangesAsync();

    public async Task DeleteAsync(Vendor vendor)
    {
        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
    }

    // IgnoreQueryFilters — a soft-deleted PurchaseOrder row still physically references
    // this Vendor (its FK doesn't go away), and the DB's Restrict constraint enforces
    // that regardless of IsDeleted. Filtering here would let this check pass, then crash
    // on the actual DELETE with a raw FK violation instead of this clean 409.
    public Task<bool> HasPurchaseOrdersAsync(int vendorId) =>
        _context.PurchaseOrders.IgnoreQueryFilters().AnyAsync(po => po.VendorId == vendorId);
}
