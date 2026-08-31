namespace EquipmentAcquisition.Core.Repositories.Interfaces;

/// <summary>Resolution happens at write time, not refresh time — each method
/// resolves the affected AcquisitionRequestId(s) for its source table and
/// enqueues them directly. See table-design.md's Orchestration section.</summary>
public interface ICacheRefreshQueueRepository
{
    Task EnqueueForRequestAsync(int acquisitionRequestId);
    Task EnqueueForVendorAsync(int vendorId);
    Task EnqueueForDepartmentAsync(int departmentId);
    Task EnqueueForEquipmentCategoryAsync(int equipmentCategoryId);
    Task EnqueueForEmployeeAsync(int employeeId);
}
