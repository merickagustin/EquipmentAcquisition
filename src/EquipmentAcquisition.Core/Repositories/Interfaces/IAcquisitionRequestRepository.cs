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

    // Tracked (not AsNoTracking) — callers mutate the returned entities in place
    // before calling SaveApprovalBatchAsync.
    Task<List<AcquisitionRequest>> GetByIdsAsync(int[] ids);

    // Commits every request mutation already tracked on the context (from a prior
    // GetByIdsAsync) plus the given audit rows and cache-refresh signals in one
    // SaveChangesAsync — one transaction covering the whole batch, not one per row.
    Task SaveApprovalBatchAsync(IEnumerable<AuditTrail> auditRows, IEnumerable<int> approvedRequestIds);
}
