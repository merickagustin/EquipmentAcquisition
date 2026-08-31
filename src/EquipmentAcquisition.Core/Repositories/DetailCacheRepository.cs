using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using CacheEntity = EquipmentAcquisition.Domain.Entities.EquipmentAcquisitionDetailCache;

namespace EquipmentAcquisition.Core.Repositories;

public class DetailCacheRepository : IDetailCacheRepository
{
    private readonly AppDbContext _context;

    public DetailCacheRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<RequestDetailDto>> GetPagedAsync(RequestListQuery query)
    {
        // Mandatory triad first — Department/Status/Date are never null, matching the
        // (DepartmentId, Status, RequestDate) index. Optional filters appended only when
        // actually supplied — never a catch-all `(@Param IS NULL OR Col = @Param)`, which
        // would defeat the index for every request regardless of what's actually filtered.
        var filtered = _context.Set<CacheEntity>().AsNoTracking()
            .Where(c => c.DepartmentId == query.DepartmentId
                     && c.Status == query.Status
                     && c.RequestDate >= query.From && c.RequestDate <= query.To);

        if (query.EquipmentCategoryId is not null)
            filtered = filtered.Where(c => c.EquipmentCategoryId == query.EquipmentCategoryId);
        if (query.VendorId is not null)
            filtered = filtered.Where(c => c.VendorId == query.VendorId);
        if (query.RequestedByEmployeeId is not null)
            filtered = filtered.Where(c => c.RequestedByEmployeeId == query.RequestedByEmployeeId);
        if (query.ApprovedByEmployeeId is not null)
            filtered = filtered.Where(c => c.ApprovedByEmployeeId == query.ApprovedByEmployeeId);

        var totalCount = await filtered.CountAsync();

        var sorted = query.SortBy switch
        {
            "EstimatedCost" => query.SortDescending ? filtered.OrderByDescending(c => c.EstimatedCost) : filtered.OrderBy(c => c.EstimatedCost),
            "TotalCost" => query.SortDescending ? filtered.OrderByDescending(c => c.TotalCost) : filtered.OrderBy(c => c.TotalCost),
            "VendorName" => query.SortDescending ? filtered.OrderByDescending(c => c.VendorName) : filtered.OrderBy(c => c.VendorName),
            "DepartmentName" => query.SortDescending ? filtered.OrderByDescending(c => c.DepartmentName) : filtered.OrderBy(c => c.DepartmentName),
            _ => query.SortDescending ? filtered.OrderByDescending(c => c.RequestDate) : filtered.OrderBy(c => c.RequestDate),
        };
        // Stable tiebreaker — RequestDate (or any sort column) alone isn't guaranteed unique.
        sorted = sorted.ThenBy(c => c.AcquisitionRequestId);

        var items = await sorted
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new RequestDetailDto(
                c.AcquisitionRequestId, c.DepartmentName, c.EquipmentCategoryName, c.RequestedByName,
                c.ApprovedByName, c.ItemDescription, c.Quantity, c.EstimatedCost, c.RequestDate,
                c.Status, c.VendorName, c.TotalCost, c.RefreshedAt))
            .ToListAsync();

        return new PagedResult<RequestDetailDto>(items, totalCount, query.PageNumber, query.PageSize);
    }
}
