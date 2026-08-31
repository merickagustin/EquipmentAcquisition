using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
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

    public Task<List<ReportRowDto>> GetDepartmentSpendAsync(DateTime? from, DateTime? to, int? departmentId)
    {
        // Nullable, not defaulted — a missing From/To must not silently become
        // DateTime.MinValue, which SQL Server's datetime type can't even represent
        // (floor is year 1753) and would crash with a raw SqlTypeException instead
        // of a clean 400. Same fix as RequestListQuery — see DetailCacheRepository.
        if (from is null || to is null)
            throw new ValidationException("From and To are both required.");

        return _reports.GetDepartmentSpendAsync(from.Value, to.Value, departmentId);
    }
}
