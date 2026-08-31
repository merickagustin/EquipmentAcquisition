using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IEquipmentCategoryRepository
{
    Task<List<EquipmentCategory>> GetAllAsync();
    Task<EquipmentCategory?> GetByIdAsync(int id);
    Task<EquipmentCategory> AddAsync(EquipmentCategory category);
    Task UpdateAsync(EquipmentCategory category);
    Task DeleteAsync(EquipmentCategory category);
    Task<bool> HasDependentsAsync(int categoryId);
}
