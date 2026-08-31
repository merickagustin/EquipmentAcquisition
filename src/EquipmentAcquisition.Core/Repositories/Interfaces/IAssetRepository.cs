using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IAssetRepository
{
    Task<List<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<Asset> AddAsync(Asset asset);
    Task UpdateAsync(Asset asset);
    Task DeleteAsync(Asset asset);
    Task<bool> PurchaseOrderExistsAsync(int purchaseOrderId);
    Task<bool> DepartmentExistsAsync(int departmentId);
    Task<bool> AssetTagExistsAsync(string assetTag);
}
