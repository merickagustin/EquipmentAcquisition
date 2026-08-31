using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class EquipmentCategoryService : IEquipmentCategoryService
{
    private const string TableName = "EquipmentCategory";

    private readonly IEquipmentCategoryRepository _categories;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public EquipmentCategoryService(IEquipmentCategoryRepository categories, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _categories = categories;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<EquipmentCategoryDto>> GetAllAsync() =>
        (await _categories.GetAllAsync()).Select(ToDto).ToList();

    public async Task<EquipmentCategoryDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<EquipmentCategoryDto> CreateAsync(CreateEquipmentCategoryDto dto)
    {
        var category = new EquipmentCategory { Name = dto.Name };
        await _categories.AddAsync(category);

        await _auditTrail.AddAsync(TableName, category.Id, AuditAction.Insert, null, Serialize(category));

        return ToDto(category);
    }

    public async Task<EquipmentCategoryDto> UpdateAsync(int id, UpdateEquipmentCategoryDto dto)
    {
        var category = await GetOrThrowAsync(id);
        var oldValues = Serialize(category);

        category.Name = dto.Name;
        await _categories.UpdateAsync(category);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(category));
        await _cacheRefreshQueue.EnqueueForEquipmentCategoryAsync(id);

        return ToDto(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await GetOrThrowAsync(id);

        if (await _categories.HasDependentsAsync(id))
            throw new ConflictException($"EquipmentCategory {id} has requests and cannot be deleted.");

        var oldValues = Serialize(category);
        await _categories.DeleteAsync(category);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, null);
    }

    private async Task<EquipmentCategory> GetOrThrowAsync(int id) =>
        await _categories.GetByIdAsync(id) ?? throw new NotFoundException($"EquipmentCategory {id} no longer exists.");

    private static string Serialize(EquipmentCategory category) =>
        JsonSerializer.Serialize(new { category.Name });

    private static EquipmentCategoryDto ToDto(EquipmentCategory category) => new(category.Id, category.Name);
}
