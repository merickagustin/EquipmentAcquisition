using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;

namespace EquipmentAcquisition.Core.Services;

public class MenuItemService : IMenuItemService
{
    private readonly IMenuItemRepository _menuItems;

    public MenuItemService(IMenuItemRepository menuItems)
    {
        _menuItems = menuItems;
    }

    public async Task<List<MenuItemDto>> GetAllAsync() =>
        (await _menuItems.GetAllAsync()).Select(ToDto).ToList();

    public async Task<MenuItemDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto)
    {
        if (dto.ParentId is not null && !await _menuItems.ParentExistsAsync(dto.ParentId.Value))
            throw new ValidationException($"Parent MenuItem {dto.ParentId} does not exist.");

        var menuItem = new MenuItem
        {
            ParentId = dto.ParentId,
            Label = dto.Label,
            Route = dto.Route,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };
        await _menuItems.AddAsync(menuItem);

        return ToDto(menuItem);
    }

    public async Task<MenuItemDto> UpdateAsync(int id, UpdateMenuItemDto dto)
    {
        var menuItem = await GetOrThrowAsync(id);

        if (dto.ParentId is not null)
        {
            if (!await _menuItems.ParentExistsAsync(dto.ParentId.Value))
                throw new ValidationException($"Parent MenuItem {dto.ParentId} does not exist.");
            if (dto.ParentId == id || await _menuItems.WouldCreateCycleAsync(id, dto.ParentId.Value))
                throw new ConflictException($"Reparenting MenuItem {id} under {dto.ParentId} would create a cycle.");
        }

        menuItem.ParentId = dto.ParentId;
        menuItem.Label = dto.Label;
        menuItem.Route = dto.Route;
        menuItem.DisplayOrder = dto.DisplayOrder;
        menuItem.IsActive = dto.IsActive;
        await _menuItems.UpdateAsync(menuItem);

        return ToDto(menuItem);
    }

    public async Task DeleteAsync(int id)
    {
        var menuItem = await GetOrThrowAsync(id);

        if (await _menuItems.HasChildrenAsync(id))
            throw new ConflictException($"MenuItem {id} has children and cannot be deleted.");

        await _menuItems.DeleteAsync(menuItem);
    }

    private async Task<MenuItem> GetOrThrowAsync(int id) =>
        await _menuItems.GetByIdAsync(id) ?? throw new NotFoundException($"MenuItem {id} no longer exists.");

    private static MenuItemDto ToDto(MenuItem m) => new(m.Id, m.ParentId, m.Label, m.Route, m.DisplayOrder, m.IsActive);
}
