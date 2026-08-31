using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Dtos;

public record AssetDto(
    int Id, int PurchaseOrderId, int DepartmentId, string AssetTag, string? SerialNumber,
    AssetStatus Status, DateTime AcquiredDate, DateTime LastUpdated);

public record CreateAssetDto(int PurchaseOrderId, int DepartmentId, string AssetTag, string? SerialNumber, AssetStatus Status);

public record UpdateAssetDto(int DepartmentId, AssetStatus Status);
