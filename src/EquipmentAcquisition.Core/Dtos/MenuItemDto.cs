namespace EquipmentAcquisition.Core.Dtos;

public record MenuItemDto(int Id, int? ParentId, string Label, string? Route, int DisplayOrder, bool IsActive);

public record CreateMenuItemDto(int? ParentId, string Label, string? Route, int DisplayOrder, bool IsActive);

public record UpdateMenuItemDto(int? ParentId, string Label, string? Route, int DisplayOrder, bool IsActive);
