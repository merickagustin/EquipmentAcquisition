using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly AppDbContext _context;

    public AssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Asset>> GetAllAsync() =>
        _context.Assets.AsNoTracking().OrderByDescending(a => a.AcquiredDate).ToListAsync();

    public async Task<(List<Asset> Items, int TotalCount)> GetPagedAsync(AssetListQuery query)
    {
        // Filters appended only when actually supplied — same reasoning as
        // DetailCacheRepository: never a catch-all `(@Param IS NULL OR Col = @Param)`.
        var filtered = _context.Assets.AsNoTracking().AsQueryable();
        if (query.DepartmentId is not null)
            filtered = filtered.Where(a => a.DepartmentId == query.DepartmentId);
        if (query.PurchaseOrderId is not null)
            filtered = filtered.Where(a => a.PurchaseOrderId == query.PurchaseOrderId);
        if (query.Status is not null)
            filtered = filtered.Where(a => a.Status == query.Status);

        var totalCount = await filtered.CountAsync();
        var items = await filtered
            .OrderByDescending(a => a.AcquiredDate)
            .ThenBy(a => a.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Asset?> GetByIdAsync(int id) =>
        _context.Assets.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Asset> AddAsync(Asset asset)
    {
        _context.Assets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }

    public Task UpdateAsync(Asset asset) => _context.SaveChangesAsync();

    public async Task DeleteAsync(Asset asset)
    {
        _context.Assets.Remove(asset);
        await _context.SaveChangesAsync();
    }

    public Task<bool> PurchaseOrderExistsAsync(int purchaseOrderId) =>
        _context.PurchaseOrders.AnyAsync(po => po.Id == purchaseOrderId);

    public Task<bool> DepartmentExistsAsync(int departmentId) =>
        _context.Departments.AnyAsync(d => d.Id == departmentId);

    public Task<bool> AssetTagExistsAsync(string assetTag) =>
        _context.Assets.AnyAsync(a => a.AssetTag == assetTag);
}
