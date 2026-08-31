using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class VendorService : IVendorService
{
    private const string TableName = "Vendor";

    private readonly IVendorRepository _vendors;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public VendorService(IVendorRepository vendors, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _vendors = vendors;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<VendorDto>> GetAllAsync() =>
        (await _vendors.GetAllAsync()).Select(ToDto).ToList();

    public async Task<VendorDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<VendorDto> CreateAsync(CreateVendorDto dto)
    {
        var vendor = new Vendor { Name = dto.Name, ContactEmail = dto.ContactEmail };
        await _vendors.AddAsync(vendor);

        // No CacheRefreshQueue signal — a brand-new vendor has no PurchaseOrders yet, nothing to refresh.
        await _auditTrail.AddAsync(TableName, vendor.Id, AuditAction.Insert, null, Serialize(vendor));

        return ToDto(vendor);
    }

    public async Task<VendorDto> UpdateAsync(int id, UpdateVendorDto dto)
    {
        var vendor = await GetOrThrowAsync(id);
        var oldValues = Serialize(vendor);

        vendor.Name = dto.Name;
        vendor.ContactEmail = dto.ContactEmail;
        await _vendors.UpdateAsync(vendor);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(vendor));
        await _cacheRefreshQueue.EnqueueForVendorAsync(id);

        return ToDto(vendor);
    }

    public async Task DeleteAsync(int id)
    {
        var vendor = await GetOrThrowAsync(id);

        if (await _vendors.HasPurchaseOrdersAsync(id))
            throw new ConflictException($"Vendor {id} has purchase orders and cannot be deleted.");

        var oldValues = Serialize(vendor);
        await _vendors.DeleteAsync(vendor);

        // No CacheRefreshQueue signal — the conflict check above guarantees no PurchaseOrder
        // (and therefore no affected AcquisitionRequestId) references this vendor.
        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, null);
    }

    private async Task<Vendor> GetOrThrowAsync(int id) =>
        await _vendors.GetByIdAsync(id) ?? throw new NotFoundException($"Vendor {id} no longer exists.");

    private static string Serialize(Vendor vendor) =>
        JsonSerializer.Serialize(new { vendor.Name, vendor.ContactEmail });

    private static VendorDto ToDto(Vendor vendor) => new(vendor.Id, vendor.Name, vendor.ContactEmail);
}
