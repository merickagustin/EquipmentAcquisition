using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentAcquisition.Api.Controllers;

[ApiController]
[Route("api/equipment-categories")]
public class EquipmentCategoriesController : ControllerBase
{
    private readonly IEquipmentCategoryService _service;

    public EquipmentCategoriesController(IEquipmentCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<EquipmentCategoryDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EquipmentCategoryDto>> GetById(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<EquipmentCategoryDto>> Create(CreateEquipmentCategoryDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EquipmentCategoryDto>> Update(int id, UpdateEquipmentCategoryDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
