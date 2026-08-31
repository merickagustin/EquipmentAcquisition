namespace EquipmentAcquisition.Core.Dtos;

public class ReportRowDto
{
    public string DepartmentName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public int RequestCount { get; set; }
    public decimal TotalSpend { get; set; }
}
