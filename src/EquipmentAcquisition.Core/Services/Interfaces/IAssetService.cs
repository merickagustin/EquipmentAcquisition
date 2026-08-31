using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IAssetService
{
    Task<List<AssetDto>> GetAllAsync();
    Task<PagedResult<AssetDto>> GetPagedAsync(AssetListQuery query);
    Task<AssetDto> GetByIdAsync(int id);
    Task<AssetDto> CreateAsync(CreateAssetDto dto);
    Task<AssetDto> UpdateAsync(int id, UpdateAssetDto dto);
    Task DeleteAsync(int id);
}
