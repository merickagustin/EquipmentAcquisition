namespace EquipmentAcquisition.Core.Dtos;

public record PurchaseOrderDto(
    int Id, int AcquisitionRequestId, int VendorId, string PoNumber,
    int Quantity, decimal UnitCost, decimal TotalCost, DateTime OrderDate);

// No PoNumber — generated server-side (PurchaseOrderService.CreateAsync), not
// client-supplied. See PurchaseOrderDto for the generated value.
public record CreatePurchaseOrderDto(int AcquisitionRequestId, int VendorId, int Quantity, decimal UnitCost);

public record UpdatePurchaseOrderDto(int VendorId, int Quantity, decimal UnitCost);

// All filters optional — same reasoning as AssetListQuery, not RequestListQuery:
// PurchaseOrders has ~15k rows, no multi-join read model, no mandatory-triad index.
public record PurchaseOrderListQuery(
    int? VendorId = null, int? AcquisitionRequestId = null,
    int PageNumber = 1, int PageSize = 25);

// Backs the Create dialog's request picker — Approved requests with no PO yet.
// A projection, not the full AcquisitionRequestDto: enough to render an
// unambiguous dropdown label (item, department, requester, qty, cost), not an
// edit form.
public record EligibleRequestDto(
    int Id, string ItemDescription, string DepartmentName, string RequestedByName,
    int Quantity, decimal EstimatedCost, DateTime ApprovedDate);
