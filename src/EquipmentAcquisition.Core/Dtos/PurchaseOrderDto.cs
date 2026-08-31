namespace EquipmentAcquisition.Core.Dtos;

public record PurchaseOrderDto(
    int Id, int AcquisitionRequestId, int VendorId, string PoNumber,
    int Quantity, decimal UnitCost, decimal TotalCost, DateTime OrderDate);

public record CreatePurchaseOrderDto(int AcquisitionRequestId, int VendorId, string PoNumber, int Quantity, decimal UnitCost);

public record UpdatePurchaseOrderDto(int VendorId, int Quantity, decimal UnitCost);
