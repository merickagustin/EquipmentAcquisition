using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IVendorService
{
    Task<List<VendorDto>> GetAllAsync();
    Task<VendorDto> GetByIdAsync(int id);
    Task<VendorDto> CreateAsync(CreateVendorDto dto);
    Task<VendorDto> UpdateAsync(int id, UpdateVendorDto dto);
    Task DeleteAsync(int id);
}
