using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly AppDbContext _context;

    public PurchaseOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<PurchaseOrder>> GetAllAsync() =>
        _context.PurchaseOrders.AsNoTracking().OrderByDescending(po => po.OrderDate).ToListAsync();

    public Task<PurchaseOrder?> GetByIdAsync(int id) =>
        _context.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id);

    public async Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public Task UpdateAsync(PurchaseOrder purchaseOrder) => _context.SaveChangesAsync();

    public async Task DeleteAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Remove(purchaseOrder);
        await _context.SaveChangesAsync();
    }

    public Task<AcquisitionRequest?> GetRequestAsync(int acquisitionRequestId) =>
        _context.AcquisitionRequests.FirstOrDefaultAsync(r => r.Id == acquisitionRequestId);

    public Task<bool> VendorExistsAsync(int vendorId) =>
        _context.Vendors.AnyAsync(v => v.Id == vendorId);

    public Task<bool> RequestAlreadyHasPurchaseOrderAsync(int acquisitionRequestId) =>
        _context.PurchaseOrders.AnyAsync(po => po.AcquisitionRequestId == acquisitionRequestId);

    public Task<bool> HasAssetsAsync(int purchaseOrderId) =>
        _context.Assets.AnyAsync(a => a.PurchaseOrderId == purchaseOrderId);
}
