using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IReportService
{
    Task<List<ReportRowDto>> GetDepartmentSpendAsync(DateTime from, DateTime to, int? departmentId);
}
