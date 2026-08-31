using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Domain.Entities;

/// <summary>Cross-cutting — records what changed and when, not yet who (no auth
/// in this project). See table-design.md for the full design rationale.</summary>
public class AuditTrail
{
    public long Id { get; set; }
    public string TableAffected { get; set; } = null!;
    public int AffectedId { get; set; }
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? ChangedByEmployeeId { get; set; }
    public DateTime DateApplied { get; set; }

    public Employee? ChangedByEmployee { get; set; }
}
