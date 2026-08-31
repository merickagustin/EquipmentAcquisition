namespace EquipmentAcquisition.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string FullName { get; set; } = null!;
    public string? JobTitle { get; set; }

    public Department Department { get; set; } = null!;
}
