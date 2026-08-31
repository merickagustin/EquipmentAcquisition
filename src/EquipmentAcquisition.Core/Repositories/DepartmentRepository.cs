using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Department>> GetAllAsync() =>
        _context.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();

    public Task<Department?> GetByIdAsync(int id) =>
        _context.Departments.FirstOrDefaultAsync(d => d.Id == id);

    public async Task<Department> AddAsync(Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return department;
    }

    public Task UpdateAsync(Department department) => _context.SaveChangesAsync();

    public async Task DeleteAsync(Department department)
    {
        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();
    }

    // Department is referenced (Restrict) from Employee, AcquisitionRequest, and Asset.
    public async Task<bool> HasDependentsAsync(int departmentId) =>
        await _context.Employees.AnyAsync(e => e.DepartmentId == departmentId)
        || await _context.AcquisitionRequests.AnyAsync(r => r.DepartmentId == departmentId)
        || await _context.Assets.AnyAsync(a => a.DepartmentId == departmentId);
}
