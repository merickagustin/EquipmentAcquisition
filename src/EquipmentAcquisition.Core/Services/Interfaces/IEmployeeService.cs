using EquipmentAcquisition.Core.Dtos;

namespace EquipmentAcquisition.Core.Services.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto);
    Task DeleteAsync(int id);
}
