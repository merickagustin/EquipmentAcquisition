using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EquipmentAcquisition.Core.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<ReportRowDto>> GetDepartmentSpendAsync(DateTime from, DateTime to, int? departmentId) =>
        _context.ReportRows.FromSqlRaw(
            "EXEC dbo.usp_GetDepartmentSpendReport @From = {0}, @To = {1}, @DepartmentId = {2}",
            new SqlParameter("From", from),
            new SqlParameter("To", to),
            new SqlParameter("DepartmentId", (object?)departmentId ?? DBNull.Value))
            .ToListAsync();
}
