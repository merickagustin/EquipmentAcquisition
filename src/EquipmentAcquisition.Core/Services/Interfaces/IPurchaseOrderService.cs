using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> GetAllAsync();
    Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(PurchaseOrderListQuery query);
    Task<PurchaseOrderDto> GetByIdAsync(int id);
    Task<PurchaseOrderDto?> GetByAcquisitionRequestIdAsync(int acquisitionRequestId);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderDto dto);
    Task DeleteAsync(int id);
}
