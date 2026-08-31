using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetAllAsync();
    Task<PurchaseOrderDto> GetByIdAsync(int id);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderDto dto);
    Task DeleteAsync(int id);
}
