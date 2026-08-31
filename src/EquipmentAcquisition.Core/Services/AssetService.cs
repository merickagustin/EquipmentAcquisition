using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class AssetService : IAssetService
{
    private const string TableName = "Asset";

    private readonly IAssetRepository _assets;
    private readonly IAuditTrailRepository _auditTrail;

    public AssetService(IAssetRepository assets, IAuditTrailRepository auditTrail)
    {
        _assets = assets;
        _auditTrail = auditTrail;
    }

    public async Task<List<AssetDto>> GetAllAsync() =>
        (await _assets.GetAllAsync()).Select(ToDto).ToList();

    public async Task<PagedResult<AssetDto>> GetPagedAsync(AssetListQuery query)
    {
        var (items, totalCount) = await _assets.GetPagedAsync(query);
        return new PagedResult<AssetDto>(items.Select(ToDto).ToList(), totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<AssetDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<AssetDto> CreateAsync(CreateAssetDto dto)
    {
        if (!await _assets.PurchaseOrderExistsAsync(dto.PurchaseOrderId))
            throw new ValidationException($"PurchaseOrder {dto.PurchaseOrderId} does not exist.");
        if (!await _assets.DepartmentExistsAsync(dto.DepartmentId))
            throw new ValidationException($"Department {dto.DepartmentId} does not exist.");
        if (await _assets.AssetTagExistsAsync(dto.AssetTag))
            throw new ConflictException($"AssetTag '{dto.AssetTag}' is already in use.");

        var now = DateTime.UtcNow;
        var asset = new Asset
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            DepartmentId = dto.DepartmentId,
            AssetTag = dto.AssetTag,
            SerialNumber = dto.SerialNumber,
            Status = dto.Status,
            AcquiredDate = now,
            LastUpdated = now
        };
        await _assets.AddAsync(asset);

        // No CacheRefreshQueue signal — Asset isn't part of EquipmentAcquisitionDetailCache's columns.
        await _auditTrail.AddAsync(TableName, asset.Id, AuditAction.Insert, null, Serialize(asset));

        return ToDto(asset);
    }

    public async Task<AssetDto> UpdateAsync(int id, UpdateAssetDto dto)
    {
        var asset = await GetOrThrowAsync(id);
        if (!await _assets.DepartmentExistsAsync(dto.DepartmentId))
            throw new ValidationException($"Department {dto.DepartmentId} does not exist.");

        var oldValues = Serialize(asset);

        asset.DepartmentId = dto.DepartmentId;
        asset.Status = dto.Status;
        asset.LastUpdated = DateTime.UtcNow;
        await _assets.UpdateAsync(asset);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(asset));

        return ToDto(asset);
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await GetOrThrowAsync(id);
        var oldValues = Serialize(asset);
        await _assets.DeleteAsync(asset);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, null);
    }

    private async Task<Asset> GetOrThrowAsync(int id) =>
        await _assets.GetByIdAsync(id) ?? throw new NotFoundException($"Asset {id} no longer exists.");

    private static string Serialize(Asset a) => JsonSerializer.Serialize(new
    {
        a.PurchaseOrderId, a.DepartmentId, a.AssetTag, a.SerialNumber, a.Status, a.AcquiredDate, a.LastUpdated
    });

    private static AssetDto ToDto(Asset a) => new(
        a.Id, a.PurchaseOrderId, a.DepartmentId, a.AssetTag, a.SerialNumber, a.Status, a.AcquiredDate, a.LastUpdated);
}
