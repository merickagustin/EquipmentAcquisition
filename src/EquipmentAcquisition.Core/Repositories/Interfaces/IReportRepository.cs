using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IReportRepository
{
    Task<List<ReportRowDto>> GetDepartmentSpendAsync(DateTime from, DateTime to, int? departmentId);
}
