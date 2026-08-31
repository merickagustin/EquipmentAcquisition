using System.Text.Json;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using EquipmentAcquisition.Domain.Entities;
using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private const string TableName = "PurchaseOrder";

    private readonly IPurchaseOrderRepository _purchaseOrders;
    private readonly ICacheRefreshQueueRepository _cacheRefreshQueue;
    private readonly IAuditTrailRepository _auditTrail;

    public PurchaseOrderService(IPurchaseOrderRepository purchaseOrders, ICacheRefreshQueueRepository cacheRefreshQueue, IAuditTrailRepository auditTrail)
    {
        _purchaseOrders = purchaseOrders;
        _cacheRefreshQueue = cacheRefreshQueue;
        _auditTrail = auditTrail;
    }

    public async Task<List<PurchaseOrderDto>> GetAllAsync() =>
        (await _purchaseOrders.GetAllAsync()).Select(ToDto).ToList();

    public async Task<PagedResult<PurchaseOrderDto>> GetPagedAsync(PurchaseOrderListQuery query)
    {
        var (items, totalCount) = await _purchaseOrders.GetPagedAsync(query);
        return new PagedResult<PurchaseOrderDto>(items.Select(ToDto).ToList(), totalCount, query.PageNumber, query.PageSize);
    }

    public Task<List<EligibleRequestDto>> GetEligibleRequestsAsync() =>
        _purchaseOrders.GetApprovedWithoutPurchaseOrderAsync();

    public async Task<PurchaseOrderDto> GetByIdAsync(int id) =>
        ToDto(await GetOrThrowAsync(id));

    public async Task<PurchaseOrderDto?> GetByAcquisitionRequestIdAsync(int acquisitionRequestId)
    {
        var purchaseOrder = await _purchaseOrders.GetByAcquisitionRequestIdAsync(acquisitionRequestId);
        return purchaseOrder is null ? null : ToDto(purchaseOrder);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        var request = await _purchaseOrders.GetRequestAsync(dto.AcquisitionRequestId)
            ?? throw new ValidationException($"AcquisitionRequest {dto.AcquisitionRequestId} does not exist.");
        if (request.Status != AcquisitionRequestStatus.Approved)
            throw new ConflictException($"AcquisitionRequest {dto.AcquisitionRequestId} is not Approved; cannot create a purchase order for it.");
        if (await _purchaseOrders.RequestAlreadyHasPurchaseOrderAsync(dto.AcquisitionRequestId))
            throw new ConflictException($"AcquisitionRequest {dto.AcquisitionRequestId} already has a purchase order.");
        if (!await _purchaseOrders.VendorExistsAsync(dto.VendorId))
            throw new ValidationException($"Vendor {dto.VendorId} does not exist.");
        if (dto.Quantity < 1)
            throw new ValidationException("Quantity must be at least 1.");

        // PoNumber is generated, not client-supplied — it needs the row's own identity
        // Id, which SQL Server doesn't assign until the INSERT commits. Two saves: once
        // to get the Id, once to persist the number derived from it. Same format the
        // seeder uses (PO-{year}-{id:D6}), so a generated and a seeded PO are indistinguishable.
        var purchaseOrder = new PurchaseOrder
        {
            AcquisitionRequestId = dto.AcquisitionRequestId,
            VendorId = dto.VendorId,
            PoNumber = string.Empty,
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            TotalCost = Math.Round(dto.UnitCost * dto.Quantity, 2),
            OrderDate = DateTime.UtcNow
        };
        await _purchaseOrders.AddAsync(purchaseOrder);

        purchaseOrder.PoNumber = $"PO-{purchaseOrder.OrderDate.Year}-{purchaseOrder.Id:D6}";
        await _purchaseOrders.UpdateAsync(purchaseOrder);

        await _auditTrail.AddAsync(TableName, purchaseOrder.Id, AuditAction.Insert, null, Serialize(purchaseOrder));
        await _cacheRefreshQueue.EnqueueForRequestAsync(dto.AcquisitionRequestId);

        return ToDto(purchaseOrder);
    }

    public async Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderDto dto)
    {
        var purchaseOrder = await GetOrThrowAsync(id);
        if (!await _purchaseOrders.VendorExistsAsync(dto.VendorId))
            throw new ValidationException($"Vendor {dto.VendorId} does not exist.");
        if (dto.Quantity < 1)
            throw new ValidationException("Quantity must be at least 1.");

        var oldValues = Serialize(purchaseOrder);

        purchaseOrder.VendorId = dto.VendorId;
        purchaseOrder.Quantity = dto.Quantity;
        purchaseOrder.UnitCost = dto.UnitCost;
        purchaseOrder.TotalCost = Math.Round(dto.UnitCost * dto.Quantity, 2);
        await _purchaseOrders.UpdateAsync(purchaseOrder);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Update, oldValues, Serialize(purchaseOrder));
        await _cacheRefreshQueue.EnqueueForRequestAsync(purchaseOrder.AcquisitionRequestId);

        return ToDto(purchaseOrder);
    }

    // Soft delete, same reasoning as AcquisitionRequest — a PO is real business history
    // (it may already have assets tracked through it), so retiring it from view keeps
    // that history instead of erasing it. The filtered unique index on
    // AcquisitionRequestId (WHERE IsDeleted = 0) is what allows a replacement PO to be
    // created afterward without colliding with this now-inactive row.
    public async Task DeleteAsync(int id)
    {
        var purchaseOrder = await GetOrThrowAsync(id);

        if (await _purchaseOrders.HasAssetsAsync(id))
            throw new ConflictException($"PurchaseOrder {id} has assets and cannot be deleted.");

        var oldValues = Serialize(purchaseOrder);
        var requestId = purchaseOrder.AcquisitionRequestId;
        purchaseOrder.IsDeleted = true;
        await _purchaseOrders.UpdateAsync(purchaseOrder);

        await _auditTrail.AddAsync(TableName, id, AuditAction.Delete, oldValues, Serialize(purchaseOrder));
        await _cacheRefreshQueue.EnqueueForRequestAsync(requestId);
    }

    private async Task<PurchaseOrder> GetOrThrowAsync(int id) =>
        await _purchaseOrders.GetByIdAsync(id) ?? throw new NotFoundException($"PurchaseOrder {id} no longer exists.");

    private static string Serialize(PurchaseOrder po) => JsonSerializer.Serialize(new
    {
        po.AcquisitionRequestId, po.VendorId, po.PoNumber, po.Quantity, po.UnitCost, po.TotalCost, po.OrderDate, po.IsDeleted
    });

    private static PurchaseOrderDto ToDto(PurchaseOrder po) => new(
        po.Id, po.AcquisitionRequestId, po.VendorId, po.PoNumber, po.Quantity, po.UnitCost, po.TotalCost, po.OrderDate);
}
