namespace EquipmentAcquisition.Domain.Entities;

public class MenuItem
{
    public int Id { get; set; }

    /// <summary>Self-referencing, nullable — null = top-level item.</summary>
    public int? ParentId { get; set; }

    public string Label { get; set; } = null!;

    /// <summary>Nullable — group-header rows (e.g. "Acquisitions") expand rather
    /// than navigate. See table-design.md's MenuItem seed notes.</summary>
    public string? Route { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }

    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}
