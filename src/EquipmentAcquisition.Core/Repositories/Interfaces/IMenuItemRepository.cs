using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IMenuItemRepository
{
    Task<List<MenuItem>> GetAllAsync();
    Task<MenuItem?> GetByIdAsync(int id);
    Task<MenuItem> AddAsync(MenuItem menuItem);
    Task UpdateAsync(MenuItem menuItem);
    Task DeleteAsync(MenuItem menuItem);
    Task<bool> HasChildrenAsync(int id);
    Task<bool> ParentExistsAsync(int parentId);
    Task<bool> WouldCreateCycleAsync(int id, int newParentId);
}
