namespace EquipmentAcquisition.Domain.Entities;

/// <summary>Standalone denormalized read model — no foreign keys in either
/// direction. Every *Id below is a value copy, not a reference. See
/// table-design.md for the full design rationale and refresh orchestration.</summary>
public class EquipmentAcquisitionDetailCache
{
    public int AcquisitionRequestId { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;

    public int EquipmentCategoryId { get; set; }
    public string EquipmentCategoryName { get; set; } = null!;

    public int RequestedByEmployeeId { get; set; }
    public string RequestedByName { get; set; } = null!;
    public string? RequestedByJobTitle { get; set; }

    public int? ApprovedByEmployeeId { get; set; }
    public string? ApprovedByName { get; set; }

    public string ItemDescription { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? RejectedDate { get; set; }

    /// <summary>Materialized here despite being derived-only on AcquisitionRequest
    /// — a deliberate, documented contradiction. Written only by the refresh path.</summary>
    public byte Status { get; set; }

    public int? PurchaseOrderId { get; set; }
    public string? PoNumber { get; set; }
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public DateTime? OrderDate { get; set; }

    public DateTime RefreshedAt { get; set; }
}
