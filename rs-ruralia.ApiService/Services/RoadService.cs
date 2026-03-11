using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RoadService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "roads:";
    private const string HistoryCacheKeyPrefix = "roads:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RoadService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<Road>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Road>>(cached.ToString()!)!;
                return Result<IEnumerable<Road>>.Success(cachedData);
            }

            // Convert geography columns to text to handle invalid data like "Cul-de-sac"
            var sql = @"
                SELECT 
                    id, phase, distance_feet, start_description, end_description,
                    CAST(start_coordinates AS NVARCHAR(MAX)) as start_coordinates,
                    CAST(end_coordinates AS NVARCHAR(MAX)) as end_coordinates,
                    CAST(road_location_coordinates AS NVARCHAR(MAX)) as road_location_coordinates,
                    approved_for_maintenance, mpo, legacy_subdivision_name,
                    road_name_id, road_surface_type_id, responder_code_id, service_area_id,
                    ModifiedBy, ValidFrom, ValidTo
                FROM road";
            var result = await _db.QueryAsync<Road>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);

            return Result<IEnumerable<Road>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Road>>.Failure($"Failed to retrieve roads: {ex.Message}");
        }
    }

    public async Task<Result<Road>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<Road>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<Road>(cached.ToString()!);
                return cachedData != null 
                    ? Result<Road>.Success(cachedData)
                    : Result<Road>.Failure("Road not found");
            }

            // Convert geography columns to text to handle invalid data
            var sql = @"
                SELECT 
                    id, phase, distance_feet, start_description, end_description,
                    CAST(start_coordinates AS NVARCHAR(MAX)) as start_coordinates,
                    CAST(end_coordinates AS NVARCHAR(MAX)) as end_coordinates,
                    CAST(road_location_coordinates AS NVARCHAR(MAX)) as road_location_coordinates,
                    approved_for_maintenance, mpo, legacy_subdivision_name,
                    road_name_id, road_surface_type_id, responder_code_id, service_area_id,
                    ModifiedBy, ValidFrom, ValidTo
                FROM road 
                WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Road>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<Road>.Success(result);
            }

            return Result<Road>.Failure("Road not found");
        }
        catch (Exception ex)
        {
            return Result<Road>.Failure($"Failed to retrieve road: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Road>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<Road>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Road>>(cached.ToString()!)!;
                return Result<IEnumerable<Road>>.Success(cachedData);
            }

            // Convert geography columns to text to handle invalid data
            var sql = @"
                SELECT 
                    id, phase, distance_feet, start_description, end_description,
                    CAST(start_coordinates AS NVARCHAR(MAX)) as start_coordinates,
                    CAST(end_coordinates AS NVARCHAR(MAX)) as end_coordinates,
                    CAST(road_location_coordinates AS NVARCHAR(MAX)) as road_location_coordinates,
                    approved_for_maintenance, mpo, legacy_subdivision_name,
                    road_name_id, road_surface_type_id, responder_code_id, service_area_id,
                    ModifiedBy, ValidFrom, ValidTo
                FROM road 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";

            var result = await _db.QueryAsync<Road>(sql, new { Id = id });

            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<Road>>.Success(result);
            }

            return Result<IEnumerable<Road>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Road>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<Road>> CreateAsync(Road entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Road>.Failure(errors);
            }

            var sql = @"
                INSERT INTO road (phase, distance_feet, start_description, end_description, start_coordinates, end_coordinates, road_location_coordinates, approved_for_maintenance, mpo, legacy_subdivision_name, road_name_id, road_surface_type_id, responder_code_id, service_area_id)
                VALUES (@Phase, @DistanceFeet, @StartDescription, @EndDescription, @StartCoordinates, @EndCoordinates, 
                    CASE WHEN @RoadLocation IS NOT NULL THEN geography::STGeomFromText(@RoadLocation, 4326) ELSE NULL END,
                    @ApprovedForMaintenance, @Mpo, @LegacySubdivisionName, @RoadNameId, @RoadSurfaceTypeId, @ResponderCodeId, @ServiceAreaId);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();

            return Result<Road>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Road>.Failure($"Failed to create road: {ex.Message}");
        }
    }

    public async Task<Result<Road>> UpdateAsync(Road entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Road>.Failure(errors);
            }

            var sql = @"
                UPDATE road SET
                    phase = @Phase,
                    distance_feet = @DistanceFeet,
                    start_description = @StartDescription,
                    end_description = @EndDescription,
                    start_coordinates = @StartCoordinates,
                    end_coordinates = @EndCoordinates,
                    road_location_coordinates = CASE WHEN @RoadLocation IS NOT NULL THEN geography::STGeomFromText(@RoadLocation, 4326) ELSE NULL END,
                    approved_for_maintenance = @ApprovedForMaintenance,
                    mpo = @Mpo,
                    legacy_subdivision_name = @LegacySubdivisionName,
                    road_name_id = @RoadNameId,
                    road_surface_type_id = @RoadSurfaceTypeId,
                    responder_code_id = @ResponderCodeId,
                    service_area_id = @ServiceAreaId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);

            if (affected == 0)
                return Result<Road>.Failure("Road not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<Road>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Road>.Failure($"Failed to update road: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM road WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("Road not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete road: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(Road? entity = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");
        
        if (entity != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{entity.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{entity.Id}");
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}
