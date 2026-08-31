using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentAcquisition.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshCacheProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_RefreshAcquisitionDetailCache
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @BatchSize int = 2000;
    -- OUTPUT doesn't support DISTINCT directly — drain into a staging table (duplicates allowed),
    -- then dedup into the PK'd working table used by the rest of the batch.
    CREATE TABLE #DrainedIds (AcquisitionRequestId int NOT NULL);
    CREATE TABLE #CurrentBatchAcquisitionRequestIds (AcquisitionRequestId int NOT NULL PRIMARY KEY);

    WHILE 1 = 1
    BEGIN
        ;WITH q AS (
            SELECT TOP (@BatchSize) Id, AcquisitionRequestId
            FROM dbo.CacheRefreshQueue WITH (READPAST)
            ORDER BY Id
        )
        DELETE FROM q
        OUTPUT deleted.AcquisitionRequestId INTO #DrainedIds (AcquisitionRequestId);
        IF @@ROWCOUNT = 0 BREAK;

        INSERT INTO #CurrentBatchAcquisitionRequestIds (AcquisitionRequestId)
        SELECT DISTINCT AcquisitionRequestId FROM #DrainedIds;

        BEGIN TRANSACTION;

            DELETE c
            FROM dbo.EquipmentAcquisitionDetailCaches c
            INNER JOIN #CurrentBatchAcquisitionRequestIds b
                ON b.AcquisitionRequestId = c.AcquisitionRequestId;

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
            INNER JOIN  #CurrentBatchAcquisitionRequestIds b ON b.AcquisitionRequestId = r.Id
            INNER JOIN  dbo.Departments          d  ON d.Id  = r.DepartmentId
            INNER JOIN  dbo.EquipmentCategories  ec ON ec.Id = r.EquipmentCategoryId
            INNER JOIN  dbo.Employees            re ON re.Id = r.RequestedByEmployeeId
            LEFT  JOIN  dbo.Employees            ae ON ae.Id = r.ApprovedByEmployeeId
            LEFT  JOIN  dbo.PurchaseOrders       po ON po.AcquisitionRequestId = r.Id
            LEFT  JOIN  dbo.Vendors              v  ON v.Id  = po.VendorId
            OPTION (RECOMPILE);

        COMMIT;

        TRUNCATE TABLE #DrainedIds;
        TRUNCATE TABLE #CurrentBatchAcquisitionRequestIds;
    END
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RefreshAcquisitionDetailCache;");
        }
    }
}
