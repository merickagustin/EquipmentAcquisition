using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services;
using EquipmentAcquisition.Domain.Entities;
using Moq;

namespace EquipmentAcquisition.Tests.Services;

public class MenuItemServiceTests
{
    private readonly Mock<IMenuItemRepository> _repository = new();
    private readonly MenuItemService _sut;

    public MenuItemServiceTests()
    {
        _sut = new MenuItemService(_repository.Object);
    }

    [Fact]
    public async Task DeleteAsync_WithChildren_ThrowsConflictException()
    {
        var item = new MenuItem { Id = 1, Label = "Admin" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repository.Setup(r => r.HasChildrenAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));

        _repository.Verify(r => r.DeleteAsync(It.IsAny<MenuItem>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutChildren_Deletes()
    {
        var item = new MenuItem { Id = 1, Label = "Leaf" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repository.Setup(r => r.HasChildrenAsync(1)).ReturnsAsync(false);

        await _sut.DeleteAsync(1);

        _repository.Verify(r => r.DeleteAsync(item), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MenuItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_ParentDoesNotExist_ThrowsValidationException()
    {
        _repository.Setup(r => r.ParentExistsAsync(5)).ReturnsAsync(false);
        var dto = new CreateMenuItemDto(5, "Reports", "/reports", 1, true);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_ReparentToSelf_ThrowsConflictException()
    {
        var item = new MenuItem { Id = 1, Label = "Admin" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repository.Setup(r => r.ParentExistsAsync(1)).ReturnsAsync(true);
        var dto = new UpdateMenuItemDto(1, "Admin", null, 1, true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_ReparentCreatingCycle_ThrowsConflictException()
    {
        // 1 is being reparented under 3, but 3 is a descendant of 1 — a cycle.
        var item = new MenuItem { Id = 1, Label = "Admin" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repository.Setup(r => r.ParentExistsAsync(3)).ReturnsAsync(true);
        _repository.Setup(r => r.WouldCreateCycleAsync(1, 3)).ReturnsAsync(true);
        var dto = new UpdateMenuItemDto(3, "Admin", null, 1, true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task UpdateAsync_ValidReparent_Succeeds()
    {
        var item = new MenuItem { Id = 1, Label = "Admin", ParentId = null };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _repository.Setup(r => r.ParentExistsAsync(2)).ReturnsAsync(true);
        _repository.Setup(r => r.WouldCreateCycleAsync(1, 2)).ReturnsAsync(false);
        var dto = new UpdateMenuItemDto(2, "Admin", null, 1, true);

        var result = await _sut.UpdateAsync(1, dto);

        Assert.Equal(2, result.ParentId);
        _repository.Verify(r => r.UpdateAsync(item), Times.Once);
    }
}
