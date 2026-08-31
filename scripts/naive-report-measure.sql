SET STATISTICS IO ON;
SET STATISTICS TIME ON;
EXEC dbo.usp_GetDepartmentSpendReport @From = '2026-01-01', @To = '2026-03-31', @DepartmentId = 1;
GO
