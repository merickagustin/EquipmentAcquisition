using EquipmentAcquisition.Core.Data;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Domain.Enums;
using AuditTrailEntity = EquipmentAcquisition.Domain.Entities.AuditTrail;

namespace EquipmentAcquisition.Core.Repositories;

public class AuditTrailRepository : IAuditTrailRepository
{
    private readonly AppDbContext _context;

    public AuditTrailRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(string tableAffected, int affectedId, AuditAction action, string? oldValues, string? newValues)
    {
        _context.AuditTrail.Add(new AuditTrailEntity
        {
            TableAffected = tableAffected,
            AffectedId = affectedId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues
        });
        await _context.SaveChangesAsync();
    }
}
