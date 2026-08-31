using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IAcquisitionRequestRepository
{
    Task<List<AcquisitionRequest>> GetAllAsync();
    Task<AcquisitionRequest?> GetByIdAsync(int id);
    Task<AcquisitionRequest> AddAsync(AcquisitionRequest request);
    Task UpdateAsync(AcquisitionRequest request);
    Task<bool> DepartmentExistsAsync(int departmentId);
    Task<bool> EquipmentCategoryExistsAsync(int categoryId);
    Task<bool> EmployeeExistsAsync(int employeeId);
    Task<bool> HasPurchaseOrderAsync(int requestId);
}
