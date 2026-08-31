using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<Department> AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(Department department);
    Task<bool> HasDependentsAsync(int departmentId);
}
