using System.Data;
using AgroTrack.Data;
using AgroTrack.Enums;
using AgroTrack.Models;
using AgroTrack.Repositories.Interfaces;
using AgroTrack.Utilities;
using Microsoft.Data.SqlClient;

namespace AgroTrack.Repositories.Implementations;

public class MaterialRepository : IMaterialRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MaterialRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AgriculturalMaterial?> GetByIdAsync(int materialId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT MaterialId, MaterialCode, MaterialName, Category, Unit, Stock, 
                   MinimumStock, UnitPrice, IsActive, CreatedAt, UpdatedAt
            FROM dbo.AgriculturalMaterials
            WHERE MaterialId = @MaterialId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaterialId", materialId);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapMaterial(reader);
        }

        return null;
    }

    public async Task<AgriculturalMaterial?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT MaterialId, MaterialCode, MaterialName, Category, Unit, Stock, 
                   MinimumStock, UnitPrice, IsActive, CreatedAt, UpdatedAt
            FROM dbo.AgriculturalMaterials
            WHERE MaterialCode = @MaterialCode;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaterialCode", materialCode.Trim());

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapMaterial(reader);
        }

        return null;
    }

    public async Task<PagedResult<AgriculturalMaterial>> GetPagedAsync(
        string? searchTerm,
        MaterialCategory? categoryFilter,
        bool? isLowStockFilter,
        bool? isActiveFilter,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (categoryFilter.HasValue)
        {
            whereClauses.Add("Category = @Category");
            parameters.Add(new SqlParameter("@Category", categoryFilter.Value.ToString()));
        }

        if (isLowStockFilter.HasValue && isLowStockFilter.Value)
        {
            whereClauses.Add("Stock <= MinimumStock");
        }

        if (isActiveFilter.HasValue)
        {
            whereClauses.Add("IsActive = @IsActive");
            parameters.Add(new SqlParameter("@IsActive", isActiveFilter.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            whereClauses.Add("(MaterialCode LIKE @Search OR MaterialName LIKE @Search)");
            parameters.Add(new SqlParameter("@Search", $"%{searchTerm.Trim()}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM dbo.AgriculturalMaterials {whereSql};";
        var dataSql = $@"
            SELECT MaterialId, MaterialCode, MaterialName, Category, Unit, Stock, 
                   MinimumStock, UnitPrice, IsActive, CreatedAt, UpdatedAt
            FROM dbo.AgriculturalMaterials
            {whereSql}
            ORDER BY MaterialName ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        int totalCount;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            foreach (var p in parameters)
                countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        var items = new List<AgriculturalMaterial>();
        using (var dataCmd = new SqlCommand(dataSql, conn))
        {
            foreach (var p in parameters)
                dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

            dataCmd.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapMaterial(reader));
            }
        }

        return new PagedResult<AgriculturalMaterial>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<IReadOnlyList<AgriculturalMaterial>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT MaterialId, MaterialCode, MaterialName, Category, Unit, Stock, 
                   MinimumStock, UnitPrice, IsActive, CreatedAt, UpdatedAt
            FROM dbo.AgriculturalMaterials
            WHERE IsActive = 1
            ORDER BY MaterialName ASC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        var list = new List<AgriculturalMaterial>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapMaterial(reader));
        }

        return list;
    }

    public async Task<int> CreateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.AgriculturalMaterials 
                (MaterialCode, MaterialName, Category, Unit, Stock, MinimumStock, UnitPrice, IsActive, CreatedAt)
            OUTPUT INSERTED.MaterialId
            VALUES 
                (@MaterialCode, @MaterialName, @Category, @Unit, @Stock, @MinimumStock, @UnitPrice, @IsActive, SYSUTCDATETIME());";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@MaterialCode", material.MaterialCode.Trim());
        cmd.Parameters.AddWithValue("@MaterialName", material.MaterialName.Trim());
        cmd.Parameters.AddWithValue("@Category", material.Category.ToString());
        cmd.Parameters.AddWithValue("@Unit", material.Unit.Trim());
        cmd.Parameters.AddWithValue("@Stock", material.Stock);
        cmd.Parameters.AddWithValue("@MinimumStock", material.MinimumStock);
        cmd.Parameters.AddWithValue("@UnitPrice", material.UnitPrice);
        cmd.Parameters.AddWithValue("@IsActive", material.IsActive);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return (int)(result ?? 0);
    }

    public async Task<bool> UpdateAsync(AgriculturalMaterial material, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.AgriculturalMaterials
            SET MaterialName = @MaterialName,
                Category = @Category,
                Unit = @Unit,
                Stock = @Stock,
                MinimumStock = @MinimumStock,
                UnitPrice = @UnitPrice,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME()
            WHERE MaterialId = @MaterialId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@MaterialId", material.MaterialId);
        cmd.Parameters.AddWithValue("@MaterialName", material.MaterialName.Trim());
        cmd.Parameters.AddWithValue("@Category", material.Category.ToString());
        cmd.Parameters.AddWithValue("@Unit", material.Unit.Trim());
        cmd.Parameters.AddWithValue("@Stock", material.Stock);
        cmd.Parameters.AddWithValue("@MinimumStock", material.MinimumStock);
        cmd.Parameters.AddWithValue("@UnitPrice", material.UnitPrice);
        cmd.Parameters.AddWithValue("@IsActive", material.IsActive);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int materialId, CancellationToken cancellationToken = default)
    {
        const string sql = @"DELETE FROM dbo.AgriculturalMaterials WHERE MaterialId = @MaterialId;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@MaterialId", materialId);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> RecordUsageWithStockDeductionAsync(MaterialUsage usage, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = conn.BeginTransaction(IsolationLevel.RepeatableRead);

        try
        {
            // 1. Lock and Check current stock
            const string checkStockSql = @"
                SELECT Stock, UnitPrice 
                FROM dbo.AgriculturalMaterials WITH (UPDLOCK) 
                WHERE MaterialId = @MaterialId;";

            decimal currentStock;
            decimal currentUnitPrice;

            using (var checkCmd = new SqlCommand(checkStockSql, conn, transaction))
            {
                checkCmd.Parameters.AddWithValue("@MaterialId", usage.MaterialId);
                using var reader = await checkCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                currentStock = reader.GetDecimal(0);
                currentUnitPrice = reader.GetDecimal(1);
            }

            if (currentStock < usage.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false; // Insufficient stock!
            }

            // Set snapshot price if not set
            if (usage.PriceAtUsage <= 0)
                usage.PriceAtUsage = currentUnitPrice;

            var subtotal = usage.Quantity * usage.PriceAtUsage;

            // 2. Deduct stock atomically
            const string updateStockSql = @"
                UPDATE dbo.AgriculturalMaterials
                SET Stock = Stock - @Quantity,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE MaterialId = @MaterialId;";

            using (var updateCmd = new SqlCommand(updateStockSql, conn, transaction))
            {
                updateCmd.Parameters.AddWithValue("@Quantity", usage.Quantity);
                updateCmd.Parameters.AddWithValue("@MaterialId", usage.MaterialId);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 3. Insert usage record
            const string insertUsageSql = @"
                INSERT INTO dbo.MaterialUsages 
                    (ActivityId, MaterialId, Quantity, PriceAtUsage, Subtotal, CreatedAt, CreatedBy)
                OUTPUT INSERTED.MaterialUsageId
                VALUES 
                    (@ActivityId, @MaterialId, @Quantity, @PriceAtUsage, @Subtotal, SYSUTCDATETIME(), @CreatedBy);";

            using (var insertCmd = new SqlCommand(insertUsageSql, conn, transaction))
            {
                insertCmd.Parameters.AddWithValue("@ActivityId", usage.ActivityId);
                insertCmd.Parameters.AddWithValue("@MaterialId", usage.MaterialId);
                insertCmd.Parameters.AddWithValue("@Quantity", usage.Quantity);
                insertCmd.Parameters.AddWithValue("@PriceAtUsage", usage.PriceAtUsage);
                insertCmd.Parameters.AddWithValue("@Subtotal", subtotal);
                insertCmd.Parameters.AddWithValue("@CreatedBy", usage.CreatedBy);
                var newId = await insertCmd.ExecuteScalarAsync(cancellationToken);
                usage.MaterialUsageId = (int)(newId ?? 0);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> RevertUsageWithStockRestorationAsync(int materialUsageId, CancellationToken cancellationToken = default)
    {
        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = conn.BeginTransaction(IsolationLevel.RepeatableRead);

        try
        {
            // 1. Get usage record details
            const string getUsageSql = @"
                SELECT MaterialId, Quantity 
                FROM dbo.MaterialUsages WITH (UPDLOCK) 
                WHERE MaterialUsageId = @MaterialUsageId;";

            int materialId;
            decimal quantity;

            using (var getCmd = new SqlCommand(getUsageSql, conn, transaction))
            {
                getCmd.Parameters.AddWithValue("@MaterialUsageId", materialUsageId);
                using var reader = await getCmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                materialId = reader.GetInt32(0);
                quantity = reader.GetDecimal(1);
            }

            // 2. Restore stock atomically
            const string restoreStockSql = @"
                UPDATE dbo.AgriculturalMaterials
                SET Stock = Stock + @Quantity,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE MaterialId = @MaterialId;";

            using (var restoreCmd = new SqlCommand(restoreStockSql, conn, transaction))
            {
                restoreCmd.Parameters.AddWithValue("@Quantity", quantity);
                restoreCmd.Parameters.AddWithValue("@MaterialId", materialId);
                await restoreCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // 3. Delete usage record
            const string deleteUsageSql = @"DELETE FROM dbo.MaterialUsages WHERE MaterialUsageId = @MaterialUsageId;";
            using (var deleteCmd = new SqlCommand(deleteUsageSql, conn, transaction))
            {
                deleteCmd.Parameters.AddWithValue("@MaterialUsageId", materialUsageId);
                await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<MaterialUsage>> GetUsagesByActivityIdAsync(int activityId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT u.MaterialUsageId, u.ActivityId, u.MaterialId, u.Quantity, u.PriceAtUsage, u.Subtotal, u.CreatedAt, u.CreatedBy,
                   m.MaterialCode, m.MaterialName, m.Unit,
                   p.PlantingCode, p.SeasonName, a.Description AS ActivityDescription, usr.FullName AS CreatedByName
            FROM dbo.MaterialUsages u
            INNER JOIN dbo.AgriculturalMaterials m ON u.MaterialId = m.MaterialId
            INNER JOIN dbo.FarmingActivities a ON u.ActivityId = a.ActivityId
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Users usr ON u.CreatedBy = usr.UserId
            WHERE u.ActivityId = @ActivityId
            ORDER BY u.MaterialUsageId DESC;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ActivityId", activityId);

        var list = new List<MaterialUsage>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapUsage(reader));
        }

        return list;
    }

    public async Task<PagedResult<MaterialUsage>> GetUsagesPagedAsync(int? materialId, int? plantingId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<SqlParameter>();

        if (materialId.HasValue && materialId.Value > 0)
        {
            whereClauses.Add("u.MaterialId = @MaterialId");
            parameters.Add(new SqlParameter("@MaterialId", materialId.Value));
        }

        if (plantingId.HasValue && plantingId.Value > 0)
        {
            whereClauses.Add("a.PlantingId = @PlantingId");
            parameters.Add(new SqlParameter("@PlantingId", plantingId.Value));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $@"
            SELECT COUNT(*)
            FROM dbo.MaterialUsages u
            INNER JOIN dbo.FarmingActivities a ON u.ActivityId = a.ActivityId
            {whereSql};";

        var dataSql = $@"
            SELECT u.MaterialUsageId, u.ActivityId, u.MaterialId, u.Quantity, u.PriceAtUsage, u.Subtotal, u.CreatedAt, u.CreatedBy,
                   m.MaterialCode, m.MaterialName, m.Unit,
                   p.PlantingCode, p.SeasonName, a.Description AS ActivityDescription, usr.FullName AS CreatedByName
            FROM dbo.MaterialUsages u
            INNER JOIN dbo.AgriculturalMaterials m ON u.MaterialId = m.MaterialId
            INNER JOIN dbo.FarmingActivities a ON u.ActivityId = a.ActivityId
            INNER JOIN dbo.Plantings p ON a.PlantingId = p.PlantingId
            INNER JOIN dbo.Users usr ON u.CreatedBy = usr.UserId
            {whereSql}
            ORDER BY u.MaterialUsageId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        int totalCount;
        using (var countCmd = new SqlCommand(countSql, conn))
        {
            foreach (var p in parameters)
                countCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
            totalCount = (int)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0);
        }

        var list = new List<MaterialUsage>();
        using (var dataCmd = new SqlCommand(dataSql, conn))
        {
            foreach (var p in parameters)
                dataCmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));

            dataCmd.Parameters.AddWithValue("@Offset", (pageIndex - 1) * pageSize);
            dataCmd.Parameters.AddWithValue("@PageSize", pageSize);

            using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MapUsage(reader));
            }
        }

        return new PagedResult<MaterialUsage>(list, totalCount, pageIndex, pageSize);
    }

    private static AgriculturalMaterial MapMaterial(SqlDataReader reader)
    {
        return new AgriculturalMaterial
        {
            MaterialId = reader.GetInt32(reader.GetOrdinal("MaterialId")),
            MaterialCode = reader.GetString(reader.GetOrdinal("MaterialCode")),
            MaterialName = reader.GetString(reader.GetOrdinal("MaterialName")),
            Category = Enum.TryParse<MaterialCategory>(reader.GetString(reader.GetOrdinal("Category")), out var cat) ? cat : MaterialCategory.Other,
            Unit = reader.GetString(reader.GetOrdinal("Unit")),
            Stock = reader.GetDecimal(reader.GetOrdinal("Stock")),
            MinimumStock = reader.GetDecimal(reader.GetOrdinal("MinimumStock")),
            UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };
    }

    private static MaterialUsage MapUsage(SqlDataReader reader)
    {
        return new MaterialUsage
        {
            MaterialUsageId = reader.GetInt32(reader.GetOrdinal("MaterialUsageId")),
            ActivityId = reader.GetInt32(reader.GetOrdinal("ActivityId")),
            MaterialId = reader.GetInt32(reader.GetOrdinal("MaterialId")),
            Quantity = reader.GetDecimal(reader.GetOrdinal("Quantity")),
            PriceAtUsage = reader.GetDecimal(reader.GetOrdinal("PriceAtUsage")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            CreatedBy = reader.GetInt32(reader.GetOrdinal("CreatedBy")),

            MaterialCode = reader.GetString(reader.GetOrdinal("MaterialCode")),
            MaterialName = reader.GetString(reader.GetOrdinal("MaterialName")),
            Unit = reader.GetString(reader.GetOrdinal("Unit")),
            PlantingCode = reader.GetString(reader.GetOrdinal("PlantingCode")),
            SeasonName = reader.GetString(reader.GetOrdinal("SeasonName")),
            ActivityDescription = reader.GetString(reader.GetOrdinal("ActivityDescription")),
            CreatedByName = reader.GetString(reader.GetOrdinal("CreatedByName"))
        };
    }
}
