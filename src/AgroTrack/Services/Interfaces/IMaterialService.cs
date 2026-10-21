using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Utilities;

namespace AgroTrack.Services.Interfaces;

public interface IMaterialService
{
    Task<PagedResult<AgriculturalMaterial>> GetMaterialsPagedAsync(
        string? searchTerm,
        MaterialCategory? categoryFilter,
        bool? isLowStockFilter,
        bool? isActiveFilter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AgriculturalMaterial?> GetMaterialByIdAsync(int materialId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgriculturalMaterial>> GetAllActiveMaterialsAsync(CancellationToken cancellationToken = default);
    Task<Result<int>> CreateMaterialAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default);
    Task<Result> UpdateMaterialAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default);
    Task<Result> DeleteMaterialAsync(int materialId, CancellationToken cancellationToken = default);

    Task<Result> RecordMaterialUsageAsync(MaterialUsage usage, CancellationToken cancellationToken = default);
    Task<Result> CancelMaterialUsageAsync(int materialUsageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaterialUsage>> GetUsagesByActivityIdAsync(int activityId, CancellationToken cancellationToken = default);
    Task<PagedResult<MaterialUsage>> GetUsagesPagedAsync(int? materialId, int? plantingId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
