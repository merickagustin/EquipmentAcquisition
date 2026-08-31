using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class CacheRefreshQueueRepository : ICacheRefreshQueueRepository
{
    private readonly AppDbContext _context;

    public CacheRefreshQueueRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnqueueForRequestAsync(int acquisitionRequestId)
    {
        _context.CacheRefreshQueue.Add(new CacheRefreshQueue { AcquisitionRequestId = acquisitionRequestId });
        await _context.SaveChangesAsync();
    }

    public Task EnqueueForVendorAsync(int vendorId) =>
        _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
            SELECT po.AcquisitionRequestId FROM dbo.PurchaseOrders po WHERE po.VendorId = {vendorId}");

    public Task EnqueueForDepartmentAsync(int departmentId) =>
        _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
            SELECT Id FROM dbo.AcquisitionRequests WHERE DepartmentId = {departmentId}");

    public Task EnqueueForEquipmentCategoryAsync(int equipmentCategoryId) =>
        _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
            SELECT Id FROM dbo.AcquisitionRequests WHERE EquipmentCategoryId = {equipmentCategoryId}");

    public Task EnqueueForEmployeeAsync(int employeeId) =>
        _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT dbo.CacheRefreshQueue (AcquisitionRequestId)
            SELECT Id FROM dbo.AcquisitionRequests
            WHERE RequestedByEmployeeId = {employeeId} OR ApprovedByEmployeeId = {employeeId}");
}
