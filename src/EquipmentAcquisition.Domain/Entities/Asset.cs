using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Domain.Entities;

public class Asset
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }

    /// <summary>Current custodian department — can differ from the request's.</summary>
    public int DepartmentId { get; set; }

    public string AssetTag { get; set; } = null!;
    public string? SerialNumber { get; set; }
    public AssetStatus Status { get; set; }
    public DateTime AcquiredDate { get; set; }
    public DateTime LastUpdated { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
