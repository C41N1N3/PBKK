using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Repositories.Interfaces;

public interface IMaterialRepository
{
    Task<AgriculturalMaterial?> GetByIdAsync(int materialId, CancellationToken cancellationToken = default);
    Task<AgriculturalMaterial?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken = default);
    Task<PagedResult<AgriculturalMaterial>> GetPagedAsync(
        string? searchTerm,
        MaterialCategory? categoryFilter,
        bool? isLowStockFilter,
        bool? isActiveFilter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgriculturalMaterial>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int materialId, CancellationToken cancellationToken = default);

    // Transactional stock operations
    Task<bool> RecordUsageWithStockDeductionAsync(MaterialUsage usage, CancellationToken cancellationToken = default);
    Task<bool> RevertUsageWithStockRestorationAsync(int materialUsageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialUsage>> GetUsagesByActivityIdAsync(int activityId, CancellationToken cancellationToken = default);
    Task<PagedResult<MaterialUsage>> GetUsagesPagedAsync(int? materialId, int? plantingId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
