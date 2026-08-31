using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Dtos;

public record AcquisitionRequestDto(
    int Id, int DepartmentId, int EquipmentCategoryId, int RequestedByEmployeeId,
    string ItemDescription, string? Justification, int Quantity, decimal EstimatedCost,
    DateTime RequestDate, DateTime? ApprovedDate, DateTime? RejectedDate,
    int? ApprovedByEmployeeId, string? RejectionReason, AcquisitionRequestStatus Status);

public record CreateAcquisitionRequestDto(
    int DepartmentId, int EquipmentCategoryId, int RequestedByEmployeeId,
    string ItemDescription, string? Justification, int Quantity, decimal EstimatedCost);

public record UpdateAcquisitionRequestDto(
    string ItemDescription, string? Justification, int Quantity, decimal EstimatedCost);

public record ApproveAcquisitionRequestDto(int ApprovedByEmployeeId);

public record RejectAcquisitionRequestDto(string RejectionReason);
