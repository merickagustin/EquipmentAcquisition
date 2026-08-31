using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class EquipmentCategoryRepository : IEquipmentCategoryRepository
{
    private readonly AppDbContext _context;

    public EquipmentCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<EquipmentCategory>> GetAllAsync() =>
        _context.EquipmentCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

    public Task<EquipmentCategory?> GetByIdAsync(int id) =>
        _context.EquipmentCategories.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<EquipmentCategory> AddAsync(EquipmentCategory category)
    {
        _context.EquipmentCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public Task UpdateAsync(EquipmentCategory category) => _context.SaveChangesAsync();

    public async Task DeleteAsync(EquipmentCategory category)
    {
        _context.EquipmentCategories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public Task<bool> HasDependentsAsync(int categoryId) =>
        _context.AcquisitionRequests.AnyAsync(r => r.EquipmentCategoryId == categoryId);
}
