namespace EquipmentAcquisition.Domain.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public int AcquisitionRequestId { get; set; }
    public int VendorId { get; set; }
    public string PoNumber { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    /// <summary>Stored, not computed on read — see table-design.md. Kept in sync
    /// as Quantity * UnitCost by the service layer, not a DB trigger.</summary>
    public decimal TotalCost { get; set; }

    public DateTime OrderDate { get; set; }

    public AcquisitionRequest AcquisitionRequest { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
