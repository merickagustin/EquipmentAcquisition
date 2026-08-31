using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class DepartmentService : IDepartmentService
{
    private const string TableName = "Department";

    private readonly IDepartmentRepository _departments;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public DepartmentService(IDepartmentRepository departments, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _departments = departments;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<DepartmentDto>> GetAllAsync() =>
        (await _departments.GetAllAsync()).Select(ToDto).ToList();

    public async Task<DepartmentDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var department = new Department { Code = dto.Code, Name = dto.Name };
        await _departments.AddAsync(department);

        // No CacheRefreshQueue signal — a brand-new department has no requests yet.
        await _auditTrail.AddAsync(TableName, department.Id, AuditAction.Insert, null, Serialize(department));

        return ToDto(department);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentDto dto)
    {
        var department = await GetOrThrowAsync(id);
        var oldValues = Serialize(department);

        department.Code = dto.Code;
        department.Name = dto.Name;
        await _departments.UpdateAsync(department);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(department));
        await _cacheRefreshQueue.EnqueueForDepartmentAsync(id);

        return ToDto(department);
    }

    public async Task DeleteAsync(int id)
    {
        var department = await GetOrThrowAsync(id);

        if (await _departments.HasDependentsAsync(id))
            throw new ConflictException($"Department {id} has employees, requests, or assets and cannot be deleted.");

        var oldValues = Serialize(department);
        await _departments.DeleteAsync(department);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, null);
    }

    private async Task<Department> GetOrThrowAsync(int id) =>
        await _departments.GetByIdAsync(id) ?? throw new NotFoundException($"Department {id} no longer exists.");

    private static string Serialize(Department department) =>
        JsonSerializer.Serialize(new { department.Code, department.Name });

    private static DepartmentDto ToDto(Department department) => new(department.Id, department.Code, department.Name);
}
