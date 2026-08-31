using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentAcquisition.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRebuildCacheProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_RebuildAllAcquisitionDetailCache
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    TRUNCATE TABLE dbo.EquipmentAcquisitionDetailCaches;

    INSERT dbo.EquipmentAcquisitionDetailCaches
        (AcquisitionRequestId, DepartmentId, DepartmentCode, DepartmentName,
         EquipmentCategoryId, EquipmentCategoryName, RequestedByEmployeeId,
         RequestedByName, RequestedByJobTitle, ApprovedByEmployeeId, ApprovedByName,
         ItemDescription, Quantity, EstimatedCost, RequestDate, ApprovedDate,
         RejectedDate, Status, PurchaseOrderId, PoNumber, VendorId, VendorName,
         UnitCost, TotalCost, OrderDate, RefreshedAt)
    SELECT  r.Id, d.Id, d.Code, d.Name,
            ec.Id, ec.Name,
            re.Id, re.FullName, re.JobTitle,
            ae.Id, ae.FullName,
            r.ItemDescription, r.Quantity, r.EstimatedCost,
            r.RequestDate, r.ApprovedDate, r.RejectedDate,
            CASE WHEN r.RejectedDate IS NOT NULL THEN 2
                 WHEN r.ApprovedDate IS NOT NULL THEN 1
                 ELSE 0 END,
            po.Id, po.PoNumber, v.Id, v.Name,
            po.UnitCost, po.TotalCost, po.OrderDate, SYSUTCDATETIME()
    FROM        dbo.AcquisitionRequests  r
    INNER JOIN  dbo.Departments          d  ON d.Id  = r.DepartmentId
    INNER JOIN  dbo.EquipmentCategories  ec ON ec.Id = r.EquipmentCategoryId
    INNER JOIN  dbo.Employees            re ON re.Id = r.RequestedByEmployeeId
    LEFT  JOIN  dbo.Employees            ae ON ae.Id = r.ApprovedByEmployeeId
    LEFT  JOIN  dbo.PurchaseOrders       po ON po.AcquisitionRequestId = r.Id
    LEFT  JOIN  dbo.Vendors              v  ON v.Id  = po.VendorId
    OPTION (RECOMPILE);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RebuildAllAcquisitionDetailCache;");
        }
    }
}
