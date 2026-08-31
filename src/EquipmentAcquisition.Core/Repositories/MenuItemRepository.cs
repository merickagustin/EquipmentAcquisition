using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly AppDbContext _context;

    public MenuItemRepository(AppDbContext context)
    {
        _context = context;
    }

    // Flat list, ordered by ParentId, DisplayOrder — matches architecture.md's documented API surface.
    public Task<List<MenuItem>> GetAllAsync() =>
        _context.MenuItems.AsNoTracking().OrderBy(m => m.ParentId).ThenBy(m => m.DisplayOrder).ToListAsync();

    public Task<MenuItem?> GetByIdAsync(int id) =>
        _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id);

    public async Task<MenuItem> AddAsync(MenuItem menuItem)
    {
        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();
        return menuItem;
    }

    public Task UpdateAsync(MenuItem menuItem) => _context.SaveChangesAsync();

    public async Task DeleteAsync(MenuItem menuItem)
    {
        _context.MenuItems.Remove(menuItem);
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasChildrenAsync(int id) =>
        _context.MenuItems.AnyAsync(m => m.ParentId == id);

    public Task<bool> ParentExistsAsync(int parentId) =>
        _context.MenuItems.AnyAsync(m => m.Id == parentId);

    // Walk up from the proposed new parent — a cycle exists if we ever reach the item itself.
    public async Task<bool> WouldCreateCycleAsync(int id, int newParentId)
    {
        var currentId = (int?)newParentId;
        while (currentId is not null)
        {
            if (currentId == id) return true;
            currentId = await _context.MenuItems.Where(m => m.Id == currentId).Select(m => m.ParentId).FirstOrDefaultAsync();
        }
        return false;
    }
}
