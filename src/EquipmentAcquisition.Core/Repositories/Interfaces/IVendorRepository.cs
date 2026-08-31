using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IVendorRepository
{
    Task<List<Vendor>> GetAllAsync();
    Task<Vendor?> GetByIdAsync(int id);
    Task<Vendor> AddAsync(Vendor vendor);
    Task UpdateAsync(Vendor vendor);
    Task DeleteAsync(Vendor vendor);
    Task<bool> HasPurchaseOrdersAsync(int vendorId);
}
