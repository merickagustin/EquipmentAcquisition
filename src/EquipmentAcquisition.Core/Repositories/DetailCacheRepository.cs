using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Enums;
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
        if (query.DepartmentId is null || query.Status is null || query.From is null || query.To is null)
            throw new ValidationException("DepartmentId, Status, From, and To are all required.");

        // Mandatory triad first — Department/Status/Date are never null, matching the
        // (DepartmentId, Status, RequestDate) index. Optional filters appended only when
        // actually supplied — never a catch-all `(@Param IS NULL OR Col = @Param)`, which
        // would defeat the index for every request regardless of what's actually filtered.
        // !IsDeleted is unconditional, not one of the optional filters above it —
        // a soft-deleted request never shows in the grid, there's no toggle for it.
        var filtered = _context.Set<CacheEntity>().AsNoTracking()
            .Where(c => !c.IsDeleted
                     && c.DepartmentId == query.DepartmentId
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

    // Every Department appears, including zero-pending ones — a correlated count
    // per department, not a GROUP BY over the cache, so the Home widget's row count
    // is always complete rather than "however many departments happen to have a
    // pending request right now." Sorted after materializing, not in the query —
    // EF Core can't translate ORDER BY on a property of a record constructed from a
    // correlated subquery. Fine either way at 20 departments.
    public async Task<List<DepartmentPendingCountDto>> GetPendingCountsByDepartmentAsync()
    {
        var counts = await _context.Departments.AsNoTracking()
            .Select(d => new
            {
                d.Id,
                d.Name,
                PendingCount = _context.Set<CacheEntity>().Count(c =>
                    !c.IsDeleted && c.DepartmentId == d.Id && c.Status == (byte)AcquisitionRequestStatus.Pending)
            })
            .ToListAsync();

        return counts
            .OrderByDescending(x => x.PendingCount)
            .ThenBy(x => x.Name)
            .Select(x => new DepartmentPendingCountDto(x.Id, x.Name, x.PendingCount))
            .ToList();
    }
}
