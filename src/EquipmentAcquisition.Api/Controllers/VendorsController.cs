using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentAcquisition.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IVendorService _service;

    public VendorsController(IVendorService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<VendorDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendorDto>> GetById(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create(CreateVendorDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VendorDto>> Update(int id, UpdateVendorDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
