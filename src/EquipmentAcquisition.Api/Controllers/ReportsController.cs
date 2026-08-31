using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentAcquisition.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    [HttpGet("department-spend")]
    public async Task<ActionResult<List<ReportRowDto>>> GetDepartmentSpend(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? departmentId = null) =>
        Ok(await _service.GetDepartmentSpendAsync(from, to, departmentId));
}
