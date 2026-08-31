namespace EquipmentAcquisition.Core.Dtos;

public record RequestDetailDto(
    int AcquisitionRequestId, string DepartmentName, string EquipmentCategoryName,
    string RequestedByName, string? ApprovedByName, string ItemDescription, int Quantity,
    decimal EstimatedCost, DateTime RequestDate, byte Status, string? VendorName,
    decimal? TotalCost, DateTime RefreshedAt);

public record RequestListQuery(
    int? DepartmentId, byte? Status, DateTime? From, DateTime? To,
    int? EquipmentCategoryId = null, int? VendorId = null,
    int? RequestedByEmployeeId = null, int? ApprovedByEmployeeId = null,
    int PageNumber = 1, int PageSize = 50,
    string SortBy = "RequestDate", bool SortDescending = true);

// Backs the Home page's pending-per-department widget. One row per Department —
// including zero-pending ones, so the count of departments shown is always
// complete, not just the ones with something to show.
public record DepartmentPendingCountDto(int DepartmentId, string DepartmentName, int PendingCount);
