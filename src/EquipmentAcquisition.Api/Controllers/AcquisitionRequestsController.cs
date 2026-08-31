using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentAcquisition.Api.Controllers;

[ApiController]
[Route("api/acquisition-requests")]
public class AcquisitionRequestsController : ControllerBase
{
    private readonly IAcquisitionRequestService _service;
    private readonly IDetailCacheRepository _detailCache;

    public AcquisitionRequestsController(IAcquisitionRequestService service, IDetailCacheRepository detailCache)
    {
        _service = service;
        _detailCache = detailCache;
    }

    [HttpGet]
    public async Task<ActionResult<List<AcquisitionRequestDto>>> GetAll() => Ok(await _service.GetAllAsync());

    /// <summary>Paginated/filterable grid read — backed by EquipmentAcquisitionDetailCache,
    /// not the base tables. Department, Status, From, To are mandatory; the rest optional.</summary>
    [HttpGet("grid")]
    public async Task<ActionResult<PagedResult<RequestDetailDto>>> GetGrid([FromQuery] RequestListQuery query) =>
        Ok(await _detailCache.GetPagedAsync(query));

    /// <summary>Backs the Home page's pending-per-department widget — every
    /// Department, including zero-pending ones.</summary>
    [HttpGet("pending-by-department")]
    public async Task<ActionResult<List<DepartmentPendingCountDto>>> GetPendingByDepartment() =>
        Ok(await _detailCache.GetPendingCountsByDepartmentAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AcquisitionRequestDto>> GetById(int id) => Ok(await _service.GetByIdAsync(id));

    [HttpPost]
    public async Task<ActionResult<AcquisitionRequestDto>> Create(CreateAcquisitionRequestDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AcquisitionRequestDto>> Update(int id, UpdateAcquisitionRequestDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<AcquisitionRequestDto>> Approve(int id, ApproveAcquisitionRequestDto dto) => Ok(await _service.ApproveAsync(id, dto));

    /// <summary>All-or-nothing: every id must be Pending or none are approved.</summary>
    [HttpPost("approve-batch")]
    public async Task<ActionResult<List<AcquisitionRequestDto>>> ApproveBatch(BatchApproveAcquisitionRequestDto dto) =>
        Ok(await _service.ApproveBatchAsync(dto));

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<AcquisitionRequestDto>> Reject(int id, RejectAcquisitionRequestDto dto) => Ok(await _service.RejectAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
