using EquipmentAcquisition.Domain.Enums;

namespace EquipmentAcquisition.Core.Repositories.Interfaces;

public interface IAuditTrailRepository
{
    Task AddAsync(string tableAffected, int affectedId, AuditAction action, string? oldValues, string? newValues);
}
