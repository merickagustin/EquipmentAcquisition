namespace EquipmentAcquisition.Core.Dtos;

public record DepartmentDto(int Id, string Code, string Name);

public record CreateDepartmentDto(string Code, string Name);

public record UpdateDepartmentDto(string Code, string Name);
