namespace EquipmentAcquisition.Core.Dtos;

public record EmployeeDto(int Id, int DepartmentId, string FullName, string? JobTitle);

public record CreateEmployeeDto(int DepartmentId, string FullName, string? JobTitle);

public record UpdateEmployeeDto(int DepartmentId, string FullName, string? JobTitle);
