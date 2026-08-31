using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IDetailCacheRepository
{
    Task<PagedResult<RequestDetailDto>> GetPagedAsync(RequestListQuery query);
}
