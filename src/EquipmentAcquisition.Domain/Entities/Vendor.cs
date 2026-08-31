namespace EquipmentAcquisition.Domain.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ContactEmail { get; set; }
}
