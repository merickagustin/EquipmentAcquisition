using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentAcquisition.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentSpendReportProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSpendReport
    @From         datetime,
    @To           datetime,
    @DepartmentId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Two explicit query shapes, not one (@DepartmentId IS NULL OR ...) catch-all —
    -- the latter would defeat the index seek below even when @DepartmentId is supplied.
    -- See table-design.md's grid-filter discussion for the same principle.
    IF @DepartmentId IS NOT NULL
    BEGIN
        SELECT  d.Name AS DepartmentName, ec.Name AS CategoryName,
                COUNT(*) AS RequestCount, SUM(po.TotalCost) AS TotalSpend
        FROM        dbo.AcquisitionRequests r
        INNER JOIN  dbo.PurchaseOrders      po ON po.AcquisitionRequestId = r.Id
        INNER JOIN  dbo.Departments         d  ON d.Id  = r.DepartmentId
        INNER JOIN  dbo.EquipmentCategories ec ON ec.Id = r.EquipmentCategoryId
        WHERE       r.DepartmentId = @DepartmentId
                AND r.RequestDate BETWEEN @From AND @To
        GROUP BY    d.Name, ec.Name
        ORDER BY    ec.Name
        OPTION (RECOMPILE);
    END
    ELSE
    BEGIN
        SELECT  d.Name AS DepartmentName, ec.Name AS CategoryName,
                COUNT(*) AS RequestCount, SUM(po.TotalCost) AS TotalSpend
        FROM        dbo.AcquisitionRequests r
        INNER JOIN  dbo.PurchaseOrders      po ON po.AcquisitionRequestId = r.Id
        INNER JOIN  dbo.Departments         d  ON d.Id  = r.DepartmentId
        INNER JOIN  dbo.EquipmentCategories ec ON ec.Id = r.EquipmentCategoryId
        WHERE       r.RequestDate BETWEEN @From AND @To
        GROUP BY    d.Name, ec.Name
        ORDER BY    d.Name, ec.Name
        OPTION (RECOMPILE);
    END
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetDepartmentSpendReport;");
        }
    }
}
