using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Dtos;

public record AssetDto(
    int Id, int PurchaseOrderId, int DepartmentId, string AssetTag, string? SerialNumber,
    AssetStatus Status, DateTime AcquiredDate, DateTime LastUpdated);

public record CreateAssetDto(int PurchaseOrderId, int DepartmentId, string AssetTag, string? SerialNumber, AssetStatus Status);

public record UpdateAssetDto(int DepartmentId, AssetStatus Status);

// All filters optional, unlike RequestListQuery — Asset is a flat two-FK table with
// a (DepartmentId, Status) index (see AssetConfiguration), not a multi-join read
// model, so there's no equivalent "mandatory triad" performance concern here.
public record AssetListQuery(
    int? DepartmentId = null, int? PurchaseOrderId = null, AssetStatus? Status = null,
    int PageNumber = 1, int PageSize = 25);
