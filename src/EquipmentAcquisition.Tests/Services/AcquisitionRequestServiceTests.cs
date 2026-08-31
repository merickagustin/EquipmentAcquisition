using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;
using Moq;

namespace EquipmentAcquisition.Tests.Services;

public class AcquisitionRequestServiceTests
{
    private readonly Mock<IAcquisitionRequestRepository> _repository = new();
    private readonly Mock<ICacheRefreshQueueRepository> _cacheRefreshQueue = new();
    private readonly Mock<IAuditTrailRepository> _auditTrail = new();
    private readonly AcquisitionRequestService _sut;

    public AcquisitionRequestServiceTests()
    {
        _sut = new AcquisitionRequestService(_repository.Object, _cacheRefreshQueue.Object, _auditTrail.Object);
    }

    private static AcquisitionRequest PendingRequest(int id = 1) => new()
    {
        Id = id, DepartmentId = 1, EquipmentCategoryId = 1, RequestedByEmployeeId = 1,
        ItemDescription = "Laptop", Quantity = 1, EstimatedCost = 1000, RequestDate = DateTime.UtcNow
    };

    [Fact]
    public async Task ApproveAsync_WhenPending_SetsApprovedDateAndEmployee_AndEnqueuesRefresh()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _repository.Setup(r => r.EmployeeExistsAsync(2)).ReturnsAsync(true);

        var result = await _sut.ApproveAsync(1, new ApproveAcquisitionRequestDto(2));

        Assert.Equal(AcquisitionRequestStatus.Approved, result.Status);
        Assert.Equal(2, result.ApprovedByEmployeeId);
        Assert.NotNull(result.ApprovedDate);
        _cacheRefreshQueue.Verify(q => q.EnqueueForRequestAsync(1), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyApproved_ThrowsConflictException()
    {
        var request = PendingRequest();
        request.ApprovedDate = DateTime.UtcNow;
        request.ApprovedByEmployeeId = 5;
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.ApproveAsync(1, new ApproveAcquisitionRequestDto(2)));
    }

    [Fact]
    public async Task RejectAsync_WhenPending_SetsRejectedDateAndReason()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        var result = await _sut.RejectAsync(1, new RejectAcquisitionRequestDto("Over budget"));

        Assert.Equal(AcquisitionRequestStatus.Rejected, result.Status);
        Assert.Equal("Over budget", result.RejectionReason);
        Assert.Null(result.ApprovedDate);
    }

    [Fact]
    public async Task RejectAsync_WhenAlreadyRejected_ThrowsConflictException()
    {
        var request = PendingRequest();
        request.RejectedDate = DateTime.UtcNow;
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RejectAsync(1, new RejectAcquisitionRequestDto("Too late")));
    }

    [Fact]
    public async Task CreateAsync_DepartmentDoesNotExist_ThrowsValidationException()
    {
        _repository.Setup(r => r.DepartmentExistsAsync(1)).ReturnsAsync(false);
        var dto = new CreateAcquisitionRequestDto(1, 1, 1, "Laptop", null, 1, 1000);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenNotPending_ThrowsConflictException()
    {
        var request = PendingRequest();
        request.ApprovedDate = DateTime.UtcNow;
        request.ApprovedByEmployeeId = 2;
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        var dto = new UpdateAcquisitionRequestDto("Updated laptop", null, 1, 1200);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task DeleteAsync_WithPurchaseOrder_ThrowsConflictException()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _repository.Setup(r => r.HasPurchaseOrderAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_WithoutPurchaseOrder_SoftDeletes_AndEnqueuesRefresh()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _repository.Setup(r => r.HasPurchaseOrderAsync(1)).ReturnsAsync(false);

        await _sut.DeleteAsync(1);

        Assert.True(request.IsDeleted);
        _repository.Verify(r => r.UpdateAsync(request), Times.Once);
        // Enqueued even on delete — the refresh proc re-materializes the row into the cache
        // with IsDeleted = 1, and DetailCacheRepository filters it out at read time.
        _cacheRefreshQueue.Verify(q => q.EnqueueForRequestAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ThrowsNotFoundException()
    {
        // GetByIdAsync is IsDeleted-filtered at the repository level, so a second delete
        // (or any operation) against an already-deleted id sees it as gone, not present.
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((AcquisitionRequest?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(1));
    }
}
