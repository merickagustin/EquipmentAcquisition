namespace EquipmentAcquisition.Core.Dtos;

public record PurchaseOrderDto(
    int Id, int AcquisitionRequestId, int VendorId, string PoNumber,
    int Quantity, decimal UnitCost, decimal TotalCost, DateTime OrderDate);

public record CreatePurchaseOrderDto(int AcquisitionRequestId, int VendorId, string PoNumber, int Quantity, decimal UnitCost);

public record UpdatePurchaseOrderDto(int VendorId, int Quantity, decimal UnitCost);

// All filters optional — same reasoning as AssetListQuery, not RequestListQuery:
// PurchaseOrders has ~15k rows, no multi-join read model, no mandatory-triad index.
public record PurchaseOrderListQuery(
    int? VendorId = null, int? AcquisitionRequestId = null,
    int PageNumber = 1, int PageSize = 25);
