using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class EmployeeService : IEmployeeService
{
    private const string TableName = "Employee";

    private readonly IEmployeeRepository _employees;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public EmployeeService(IEmployeeRepository employees, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _employees = employees;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<EmployeeDto>> GetAllAsync() =>
        (await _employees.GetAllAsync()).Select(ToDto).ToList();

    public async Task<EmployeeDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        if (!await _employees.DepartmentExistsAsync(dto.DepartmentId))
            throw new ValidationException($"Department {dto.DepartmentId} does not exist.");

        var employee = new Employee { DepartmentId = dto.DepartmentId, FullName = dto.FullName, JobTitle = dto.JobTitle };
        await _employees.AddAsync(employee);

        // No CacheRefreshQueue signal — a brand-new employee has no requests yet.
        await _auditTrail.AddAsync(TableName, employee.Id, AuditAction.Insert, null, Serialize(employee));

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        if (!await _employees.DepartmentExistsAsync(dto.DepartmentId))
            throw new ValidationException($"Department {dto.DepartmentId} does not exist.");

        var employee = await GetOrThrowAsync(id);
        var oldValues = Serialize(employee);

        employee.DepartmentId = dto.DepartmentId;
        employee.FullName = dto.FullName;
        employee.JobTitle = dto.JobTitle;
        await _employees.UpdateAsync(employee);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(employee));
        await _cacheRefreshQueue.EnqueueForEmployeeAsync(id);

        return ToDto(employee);
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await GetOrThrowAsync(id);

        if (await _employees.HasDependentsAsync(id))
            throw new ConflictException($"Employee {id} has requests or audit history and cannot be deleted.");

        var oldValues = Serialize(employee);
        await _employees.DeleteAsync(employee);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, null);
    }

    private async Task<Employee> GetOrThrowAsync(int id) =>
        await _employees.GetByIdAsync(id) ?? throw new NotFoundException($"Employee {id} no longer exists.");

    private static string Serialize(Employee employee) =>
        JsonSerializer.Serialize(new { employee.DepartmentId, employee.FullName, employee.JobTitle });

    private static EmployeeDto ToDto(Employee employee) => new(employee.Id, employee.DepartmentId, employee.FullName, employee.JobTitle);
}
