using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class AcquisitionRequestService : IAcquisitionRequestService
{
    private const string TableName = "AcquisitionRequest";

    private readonly IAcquisitionRequestRepository _requests;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public AcquisitionRequestService(IAcquisitionRequestRepository requests, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _requests = requests;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<AcquisitionRequestDto>> GetAllAsync() =>
        (await _requests.GetAllAsync()).Select(ToDto).ToList();

    public async Task<AcquisitionRequestDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<AcquisitionRequestDto> CreateAsync(CreateAcquisitionRequestDto dto)
    {
        if (!await _requests.DepartmentExistsAsync(dto.DepartmentId))
            throw new ValidationException($"Department {dto.DepartmentId} does not exist.");
        if (!await _requests.EquipmentCategoryExistsAsync(dto.EquipmentCategoryId))
            throw new ValidationException($"EquipmentCategory {dto.EquipmentCategoryId} does not exist.");
        if (!await _requests.EmployeeExistsAsync(dto.RequestedByEmployeeId))
            throw new ValidationException($"Employee {dto.RequestedByEmployeeId} does not exist.");
        if (dto.Quantity < 1)
            throw new ValidationException("Quantity must be at least 1.");

        var request = new AcquisitionRequest
        {
            DepartmentId = dto.DepartmentId,
            EquipmentCategoryId = dto.EquipmentCategoryId,
            RequestedByEmployeeId = dto.RequestedByEmployeeId,
            ItemDescription = dto.ItemDescription,
            Justification = dto.Justification,
            Quantity = dto.Quantity,
            EstimatedCost = dto.EstimatedCost,
            RequestDate = DateTime.UtcNow
        };
        await _requests.AddAsync(request);

        await _auditTrail.AddAsync(TableName, request.Id, AuditAction.Insert, null, Serialize(request));
        await _cacheRefreshQueue.EnqueueForRequestAsync(request.Id);

        return ToDto(request);
    }

    public async Task<AcquisitionRequestDto> UpdateAsync(int id, UpdateAcquisitionRequestDto dto)
    {
        var request = await GetOrThrowAsync(id);
        if (request.Status != AcquisitionRequestStatus.Pending)
            throw new ConflictException($"AcquisitionRequest {id} is {request.Status} and can no longer be edited.");
        if (dto.Quantity < 1)
            throw new ValidationException("Quantity must be at least 1.");

        var oldValues = Serialize(request);

        request.ItemDescription = dto.ItemDescription;
        request.Justification = dto.Justification;
        request.Quantity = dto.Quantity;
        request.EstimatedCost = dto.EstimatedCost;
        await _requests.UpdateAsync(request);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(request));
        await _cacheRefreshQueue.EnqueueForRequestAsync(id);

        return ToDto(request);
    }

    public async Task<AcquisitionRequestDto> ApproveAsync(int id, ApproveAcquisitionRequestDto dto)
    {
        var request = await GetOrThrowAsync(id);
        if (request.Status != AcquisitionRequestStatus.Pending)
            throw new ConflictException($"AcquisitionRequest {id} is already {request.Status}.");
        if (!await _requests.EmployeeExistsAsync(dto.ApprovedByEmployeeId))
            throw new ValidationException($"Employee {dto.ApprovedByEmployeeId} does not exist.");

        var oldValues = Serialize(request);

        request.ApprovedDate = DateTime.UtcNow;
        request.ApprovedByEmployeeId = dto.ApprovedByEmployeeId;
        await _requests.UpdateAsync(request);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(request));
        await _cacheRefreshQueue.EnqueueForRequestAsync(id);

        return ToDto(request);
    }

    public async Task<AcquisitionRequestDto> RejectAsync(int id, RejectAcquisitionRequestDto dto)
    {
        var request = await GetOrThrowAsync(id);
        if (request.Status != AcquisitionRequestStatus.Pending)
            throw new ConflictException($"AcquisitionRequest {id} is already {request.Status}.");

        var oldValues = Serialize(request);

        request.RejectedDate = DateTime.UtcNow;
        request.RejectionReason = dto.RejectionReason;
        await _requests.UpdateAsync(request);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(request));
        await _cacheRefreshQueue.EnqueueForRequestAsync(id);

        return ToDto(request);
    }

    // Soft delete, not a physical row removal — a request is business history (it may
    // have been approved/rejected already, and its audit trail should keep meaning).
    // IsDeleted flips to true; GetByIdAsync/GetAllAsync filter it out everywhere else,
    // so the rest of the app sees exactly the same "gone" behavior as a real delete.
    public async Task DeleteAsync(int id)
    {
        var request = await GetOrThrowAsync(id);

        if (await _requests.HasPurchaseOrderAsync(id))
            throw new ConflictException($"AcquisitionRequest {id} has a purchase order and cannot be deleted.");

        var oldValues = Serialize(request);
        request.IsDeleted = true;
        await _requests.UpdateAsync(request);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, Serialize(request));

        // Enqueue even on delete: the refresh proc re-materializes this row into the cache
        // with IsDeleted = 1, and DetailCacheRepository filters those out — same practical
        // effect as the old hard-delete cleanup, but the row (and its history) still exists.
        await _cacheRefreshQueue.EnqueueForRequestAsync(id);
    }

    private async Task<AcquisitionRequest> GetOrThrowAsync(int id) =>
        await _requests.GetByIdAsync(id) ?? throw new NotFoundException($"AcquisitionRequest {id} no longer exists.");

    private static string Serialize(AcquisitionRequest r) => JsonSerializer.Serialize(new
    {
        r.DepartmentId, r.EquipmentCategoryId, r.RequestedByEmployeeId, r.ItemDescription, r.Justification,
        r.Quantity, r.EstimatedCost, r.RequestDate, r.ApprovedDate, r.RejectedDate, r.ApprovedByEmployeeId,
        r.RejectionReason, r.IsDeleted
    });

    private static AcquisitionRequestDto ToDto(AcquisitionRequest r) => new(
        r.Id, r.DepartmentId, r.EquipmentCategoryId, r.RequestedByEmployeeId,
        r.ItemDescription, r.Justification, r.Quantity, r.EstimatedCost,
        r.RequestDate, r.ApprovedDate, r.RejectedDate, r.ApprovedByEmployeeId, r.RejectionReason, r.Status);
}
