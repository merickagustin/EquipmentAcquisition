using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services.Interfaces;

namespace EquipmentAcquisition.Core.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reports;

    public ReportService(IReportRepository reports)
    {
        _reports = reports;
    }

    public Task<List<ReportRowDto>> GetDepartmentSpendAsync(DateTime from, DateTime to, int? departmentId) =>
        _reports.GetDepartmentSpendAsync(from, to, departmentId);
}
