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
