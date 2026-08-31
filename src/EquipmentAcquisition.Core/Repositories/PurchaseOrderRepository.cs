using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Dtos;
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

    public async Task<(List<PurchaseOrder> Items, int TotalCount)> GetPagedAsync(PurchaseOrderListQuery query)
    {
        var filtered = _context.PurchaseOrders.AsNoTracking().AsQueryable();
        if (query.VendorId is not null)
            filtered = filtered.Where(po => po.VendorId == query.VendorId);
        if (query.AcquisitionRequestId is not null)
            filtered = filtered.Where(po => po.AcquisitionRequestId == query.AcquisitionRequestId);

        var totalCount = await filtered.CountAsync();
        var items = await filtered
            .OrderByDescending(po => po.OrderDate)
            .ThenBy(po => po.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<PurchaseOrder?> GetByIdAsync(int id) =>
        _context.PurchaseOrders.FirstOrDefaultAsync(po => po.Id == id);

    // AcquisitionRequestId is unique on PurchaseOrders (see PurchaseOrderConfiguration) —
    // at most one row, never a list. Backs the Requests page's "already has a PO?" lookup
    // without ever fetching the full PurchaseOrders table.
    public Task<PurchaseOrder?> GetByAcquisitionRequestIdAsync(int acquisitionRequestId) =>
        _context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(po => po.AcquisitionRequestId == acquisitionRequestId);

    public async Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder)
    {
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();
        return purchaseOrder;
    }

    public Task UpdateAsync(PurchaseOrder purchaseOrder) => _context.SaveChangesAsync();

    public Task<AcquisitionRequest?> GetRequestAsync(int acquisitionRequestId) =>
        _context.AcquisitionRequests.FirstOrDefaultAsync(r => r.Id == acquisitionRequestId && !r.IsDeleted);

    // Backs the Create-PO dropdown — capped and sorted most-recently-approved-first,
    // not a full scan. In practice this stays small: the seeder gives every Approved
    // request a PO already, so only genuinely new approvals ever show up here.
    // Projected straight to the DTO (Department/RequestedByEmployee joined in one
    // query) — the dropdown label needs to identify the item unambiguously, not
    // just carry an id.
    public Task<List<EligibleRequestDto>> GetApprovedWithoutPurchaseOrderAsync() =>
        _context.AcquisitionRequests.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ApprovedDate != null && r.PurchaseOrder == null)
            .OrderByDescending(r => r.ApprovedDate)
            .Take(100)
            .Select(r => new EligibleRequestDto(
                r.Id, r.ItemDescription, r.Department.Name, r.RequestedByEmployee.FullName,
                r.Quantity, r.EstimatedCost, r.ApprovedDate!.Value))
            .ToListAsync();

    public Task<bool> VendorExistsAsync(int vendorId) =>
        _context.Vendors.AnyAsync(v => v.Id == vendorId);

    public Task<bool> RequestAlreadyHasPurchaseOrderAsync(int acquisitionRequestId) =>
        _context.PurchaseOrders.AnyAsync(po => po.AcquisitionRequestId == acquisitionRequestId);

    public Task<bool> HasAssetsAsync(int purchaseOrderId) =>
        _context.Assets.AnyAsync(a => a.PurchaseOrderId == purchaseOrderId);
}
