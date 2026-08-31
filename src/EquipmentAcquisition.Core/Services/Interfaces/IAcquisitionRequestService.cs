using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IAcquisitionRequestService
{
    Task<List<AcquisitionRequestDto>> GetAllAsync();
    Task<AcquisitionRequestDto> GetByIdAsync(int id);
    Task<AcquisitionRequestDto> CreateAsync(CreateAcquisitionRequestDto dto);
    Task<AcquisitionRequestDto> UpdateAsync(int id, UpdateAcquisitionRequestDto dto);
    Task<AcquisitionRequestDto> ApproveAsync(int id, ApproveAcquisitionRequestDto dto);
    Task<List<AcquisitionRequestDto>> ApproveBatchAsync(BatchApproveAcquisitionRequestDto dto);
    Task<AcquisitionRequestDto> RejectAsync(int id, RejectAcquisitionRequestDto dto);
    Task DeleteAsync(int id);
}
