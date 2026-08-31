using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee> AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Employee employee);
    Task<bool> DepartmentExistsAsync(int departmentId);
    Task<bool> HasDependentsAsync(int employeeId);
}
