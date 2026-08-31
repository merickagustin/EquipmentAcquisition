namespace EquipmentAcquisition.Core.Dtos;

public record EquipmentCategoryDto(int Id, string Name);

public record CreateEquipmentCategoryDto(string Name);

public record UpdateEquipmentCategoryDto(string Name);
