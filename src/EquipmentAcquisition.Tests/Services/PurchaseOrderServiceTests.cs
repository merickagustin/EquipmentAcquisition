using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;
using Moq;

namespace EquipmentAcquisition.Tests.Services;

public class PurchaseOrderServiceTests
{
    private readonly Mock<IPurchaseOrderRepository> _repository = new();
    private readonly Mock<ICacheRefreshQueueRepository> _cacheRefreshQueue = new();
    private readonly Mock<IAuditTrailRepository> _auditTrail = new();
    private readonly PurchaseOrderService _sut;

    public PurchaseOrderServiceTests()
    {
        _sut = new PurchaseOrderService(_repository.Object, _cacheRefreshQueue.Object, _auditTrail.Object);
    }

    private static AcquisitionRequest ApprovedRequest(int id = 1) => new()
    {
        Id = id, DepartmentId = 1, EquipmentCategoryId = 1, RequestedByEmployeeId = 1,
        ItemDescription = "Laptop", Quantity = 2, EstimatedCost = 2000,
        RequestDate = DateTime.UtcNow, ApprovedDate = DateTime.UtcNow, ApprovedByEmployeeId = 2
    };

    private CreatePurchaseOrderDto ValidCreateDto() => new(AcquisitionRequestId: 1, VendorId: 1, Quantity: 2, UnitCost: 100);

    [Fact]
    public async Task CreateAsync_RequestDoesNotExist_ThrowsValidationException()
    {
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync((AcquisitionRequest?)null);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(ValidCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_RequestNotApproved_ThrowsConflictException()
    {
        var pending = ApprovedRequest();
        pending.ApprovedDate = null; // Pending, not Approved
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync(pending);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(ValidCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_RequestAlreadyHasPurchaseOrder_ThrowsConflictException()
    {
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync(ApprovedRequest());
        _repository.Setup(r => r.RequestAlreadyHasPurchaseOrderAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(ValidCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_VendorDoesNotExist_ThrowsValidationException()
    {
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync(ApprovedRequest());
        _repository.Setup(r => r.RequestAlreadyHasPurchaseOrderAsync(1)).ReturnsAsync(false);
        _repository.Setup(r => r.VendorExistsAsync(1)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(ValidCreateDto()));
    }

    [Fact]
    public async Task CreateAsync_QuantityLessThanOne_ThrowsValidationException()
    {
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync(ApprovedRequest());
        _repository.Setup(r => r.RequestAlreadyHasPurchaseOrderAsync(1)).ReturnsAsync(false);
        _repository.Setup(r => r.VendorExistsAsync(1)).ReturnsAsync(true);
        var dto = ValidCreateDto() with { Quantity = 0 };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Valid_GeneratesPoNumberFromAssignedId_AndEnqueuesRefresh()
    {
        _repository.Setup(r => r.GetRequestAsync(1)).ReturnsAsync(ApprovedRequest());
        _repository.Setup(r => r.RequestAlreadyHasPurchaseOrderAsync(1)).ReturnsAsync(false);
        _repository.Setup(r => r.VendorExistsAsync(1)).ReturnsAsync(true);
        // Mirrors what SQL Server's identity column does on insert — Id isn't known
        // until AddAsync's SaveChangesAsync commits, which is exactly why PoNumber
        // generation happens in a second UpdateAsync, not at construction time.
        _repository.Setup(r => r.AddAsync(It.IsAny<PurchaseOrder>()))
            .Callback<PurchaseOrder>(po => po.Id = 555)
            .ReturnsAsync((PurchaseOrder po) => po);

        var result = await _sut.CreateAsync(ValidCreateDto());

        Assert.Equal($"PO-{DateTime.UtcNow.Year}-000555", result.PoNumber);
        Assert.Equal(200, result.TotalCost); // Quantity 2 * UnitCost 100
        _repository.Verify(r => r.UpdateAsync(It.Is<PurchaseOrder>(po => po.PoNumber == $"PO-{DateTime.UtcNow.Year}-000555")), Times.Once);
        _cacheRefreshQueue.Verify(q => q.EnqueueForRequestAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RecalculatesTotalCost_AndEnqueuesRefresh()
    {
        var po = new PurchaseOrder { Id = 1, AcquisitionRequestId = 1, VendorId = 1, PoNumber = "PO-2026-000001", Quantity = 1, UnitCost = 100, TotalCost = 100 };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(po);
        _repository.Setup(r => r.VendorExistsAsync(2)).ReturnsAsync(true);

        var result = await _sut.UpdateAsync(1, new UpdatePurchaseOrderDto(VendorId: 2, Quantity: 3, UnitCost: 50));

        Assert.Equal(150, result.TotalCost);
        Assert.Equal(2, result.VendorId);
        _cacheRefreshQueue.Verify(q => q.EnqueueForRequestAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithAssets_ThrowsConflictException_AndDoesNotSoftDelete()
    {
        var po = new PurchaseOrder { Id = 1, AcquisitionRequestId = 1, VendorId = 1, PoNumber = "PO-2026-000001", Quantity = 1, UnitCost = 100, TotalCost = 100 };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(po);
        _repository.Setup(r => r.HasAssetsAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));

        Assert.False(po.IsDeleted);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<PurchaseOrder>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutAssets_SoftDeletes_AndEnqueuesRefresh()
    {
        var po = new PurchaseOrder { Id = 1, AcquisitionRequestId = 7, VendorId = 1, PoNumber = "PO-2026-000001", Quantity = 1, UnitCost = 100, TotalCost = 100 };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(po);
        _repository.Setup(r => r.HasAssetsAsync(1)).ReturnsAsync(false);

        await _sut.DeleteAsync(1);

        Assert.True(po.IsDeleted);
        _repository.Verify(r => r.UpdateAsync(po), Times.Once);
        // Enqueued against the request, not the PO — the cache refresh re-materializes
        // the request's row with the PO fields cleared, same as a hard delete would.
        _cacheRefreshQueue.Verify(q => q.EnqueueForRequestAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((PurchaseOrder?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }
}
