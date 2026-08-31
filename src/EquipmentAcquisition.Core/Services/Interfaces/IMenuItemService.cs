using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IMenuItemService
{
    Task<List<MenuItemDto>> GetAllAsync();
    Task<MenuItemDto> GetByIdAsync(int id);
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto);
    Task<MenuItemDto> UpdateAsync(int id, UpdateMenuItemDto dto);
    Task DeleteAsync(int id);
}
