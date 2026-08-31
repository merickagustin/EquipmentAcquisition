using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class AcquisitionRequestRepository : IAcquisitionRequestRepository
{
    private readonly AppDbContext _context;

    public AcquisitionRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    // IsDeleted-filtered — a soft-deleted request behaves as "doesn't exist" for
    // every normal caller (list, GetById, edit, approve, reject). See
    // AcquisitionRequestService.DeleteAsync for where the flag actually gets set.
    public Task<List<AcquisitionRequest>> GetAllAsync() =>
        _context.AcquisitionRequests.AsNoTracking().Where(r => !r.IsDeleted).OrderByDescending(r => r.RequestDate).ToListAsync();

    public Task<AcquisitionRequest?> GetByIdAsync(int id) =>
        _context.AcquisitionRequests.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

    public async Task<AcquisitionRequest> AddAsync(AcquisitionRequest request)
    {
        _context.AcquisitionRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public Task UpdateAsync(AcquisitionRequest request) => _context.SaveChangesAsync();

    public Task<bool> DepartmentExistsAsync(int departmentId) =>
        _context.Departments.AnyAsync(d => d.Id == departmentId);

    public Task<bool> EquipmentCategoryExistsAsync(int categoryId) =>
        _context.EquipmentCategories.AnyAsync(c => c.Id == categoryId);

    public Task<bool> EmployeeExistsAsync(int employeeId) =>
        _context.Employees.AnyAsync(e => e.Id == employeeId);

    public Task<bool> HasPurchaseOrderAsync(int requestId) =>
        _context.PurchaseOrders.AnyAsync(po => po.AcquisitionRequestId == requestId);
}
