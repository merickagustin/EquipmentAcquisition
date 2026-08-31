using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Domain.Entities;

public class AcquisitionRequest
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int EquipmentCategoryId { get; set; }
    public int RequestedByEmployeeId { get; set; }
    public string ItemDescription { get; set; } = null!;
    public string? Justification { get; set; }
    public int Quantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? RejectedDate { get; set; }
    public int? ApprovedByEmployeeId { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsDeleted { get; set; }

    public Department Department { get; set; } = null!;
    public EquipmentCategory EquipmentCategory { get; set; } = null!;
    public Employee RequestedByEmployee { get; set; } = null!;
    public Employee? ApprovedByEmployee { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    /// <summary>Derived, not stored — see table-design.md.</summary>
    public AcquisitionRequestStatus Status =>
        RejectedDate.HasValue ? AcquisitionRequestStatus.Rejected :
        ApprovedDate.HasValue ? AcquisitionRequestStatus.Approved :
        AcquisitionRequestStatus.Pending;
}
