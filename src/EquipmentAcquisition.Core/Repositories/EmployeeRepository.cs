using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Employee>> GetAllAsync() =>
        _context.Employees.AsNoTracking().OrderBy(e => e.FullName).ToListAsync();

    public Task<Employee?> GetByIdAsync(int id) =>
        _context.Employees.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Employee> AddAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public Task UpdateAsync(Employee employee) => _context.SaveChangesAsync();

    public async Task DeleteAsync(Employee employee)
    {
        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
    }

    public Task<bool> DepartmentExistsAsync(int departmentId) =>
        _context.Departments.AnyAsync(d => d.Id == departmentId);

    // Employee is referenced (Restrict) from AcquisitionRequest (requester/approver) and AuditTrail.
    public async Task<bool> HasDependentsAsync(int employeeId) =>
        await _context.AcquisitionRequests.AnyAsync(r => r.RequestedByEmployeeId == employeeId || r.ApprovedByEmployeeId == employeeId)
        || await _context.AuditTrail.AnyAsync(a => a.ChangedByEmployeeId == employeeId);
}
