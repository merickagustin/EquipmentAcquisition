CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSpendReport
    @From         datetime,
    @To           datetime,
    @DepartmentId int = NULL
AS
BEGIN
    SET NOCOUNT ON;

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
END
