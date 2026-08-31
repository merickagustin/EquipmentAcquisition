using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentAcquisition.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrdersController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<PurchaseOrderDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(int id) => Ok(await _service.GetByIdAsync(id));

    // Null (not 404) when the request has no PO yet — this is a normal state for
    // an Approved request, not an error. Lets the Requests page check "does this
    // row already have a PO?" without ever fetching the full PurchaseOrders table.
    [HttpGet("by-request/{acquisitionRequestId:int}")]
    public async Task<ActionResult<PurchaseOrderDto?>> GetByRequestId(int acquisitionRequestId) =>
        Ok(await _service.GetByAcquisitionRequestIdAsync(acquisitionRequestId));

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create(CreatePurchaseOrderDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(int id, UpdatePurchaseOrderDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
