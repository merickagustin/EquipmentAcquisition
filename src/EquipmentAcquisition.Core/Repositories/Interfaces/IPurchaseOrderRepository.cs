using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<List<PurchaseOrder>> GetAllAsync();
    Task<PurchaseOrder?> GetByIdAsync(int id);
    Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder);
    Task UpdateAsync(PurchaseOrder purchaseOrder);
    Task DeleteAsync(PurchaseOrder purchaseOrder);
    Task<AcquisitionRequest?> GetRequestAsync(int acquisitionRequestId);
    Task<bool> VendorExistsAsync(int vendorId);
    Task<bool> RequestAlreadyHasPurchaseOrderAsync(int acquisitionRequestId);
    Task<bool> HasAssetsAsync(int purchaseOrderId);
}
