using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IEquipmentCategoryService
{
    Task<List<EquipmentCategoryDto>> GetAllAsync();
    Task<EquipmentCategoryDto> GetByIdAsync(int id);
    Task<EquipmentCategoryDto> CreateAsync(CreateEquipmentCategoryDto dto);
    Task<EquipmentCategoryDto> UpdateAsync(int id, UpdateEquipmentCategoryDto dto);
    Task DeleteAsync(int id);
}
