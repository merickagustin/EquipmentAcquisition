using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;
using Moq;

namespace EquipmentAcquisition.Tests.Services;

public class VendorServiceTests
{
    private readonly Mock<IVendorRepository> _repository = new();
    private readonly Mock<ICacheRefreshQueueRepository> _cacheRefreshQueue = new();
    private readonly Mock<IAuditTrailRepository> _auditTrail = new();
    private readonly VendorService _sut;

    public VendorServiceTests()
    {
        _sut = new VendorService(_repository.Object, _cacheRefreshQueue.Object, _auditTrail.Object);
    }

    [Fact]
    public async Task CreateAsync_WritesInsertAuditRow_AndDoesNotEnqueueCacheRefresh()
    {
        var dto = new CreateVendorDto("Acme Corp", "sales@acme.test");
        _repository.Setup(r => r.AddAsync(It.IsAny<Vendor>()))
            .Callback<Vendor>(v => v.Id = 51)
            .ReturnsAsync((Vendor v) => v);

        await _sut.CreateAsync(dto);

        _auditTrail.Verify(a => a.AddAsync("Vendor", 51, AuditAction.Insert, null, It.IsAny<string>()), Times.Once);
        // A brand-new vendor has no PurchaseOrders yet — nothing in the cache to refresh.
        _cacheRefreshQueue.Verify(q => q.EnqueueForVendorAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WritesUpdateAuditRow_AndEnqueuesCacheRefresh()
    {
        var vendor = new Vendor { Id = 1, Name = "Old Name", ContactEmail = "old@test.com" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vendor);
        var dto = new UpdateVendorDto("New Name", "new@test.com");

        await _sut.UpdateAsync(1, dto);

        _auditTrail.Verify(a => a.AddAsync("Vendor", 1, AuditAction.Update,
            It.Is<string>(s => s.Contains("Old Name")), It.Is<string>(s => s.Contains("New Name"))), Times.Once);
        _cacheRefreshQueue.Verify(q => q.EnqueueForVendorAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPurchaseOrders_ThrowsConflictException_AndDoesNotDelete()
    {
        var vendor = new Vendor { Id = 1, Name = "Acme" };
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vendor);
        _repository.Setup(r => r.HasPurchaseOrdersAsync(1)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(1));

        _repository.Verify(r => r.DeleteAsync(It.IsAny<Vendor>()), Times.Never);
        _auditTrail.Verify(a => a.AddAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<AuditAction>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Vendor?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }
}
