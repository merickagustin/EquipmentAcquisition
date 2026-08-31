namespace EquipmentAcquisition.Domain.Entities;

/// <summary>Working queue for EquipmentAcquisitionDetailCache refresh signals.
/// Written by each write path with the AcquisitionRequestId(s) it affects
/// already resolved — see table-design.md's Orchestration section for why
/// resolution happens at write time, not refresh time.</summary>
public class CacheRefreshQueue
{
    public long Id { get; set; }
    public int AcquisitionRequestId { get; set; }
    public DateTime EnqueuedAt { get; set; }
}
