using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RoadSurfaceTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "roadsurfacetypes:";
    private const string HistoryCacheKeyPrefix = "roadsurfacetypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RoadSurfaceTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RoadSurfaceType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadSurfaceType>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadSurfaceType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM road_surface_type";
            var result = await _db.QueryAsync<RoadSurfaceType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RoadSurfaceType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadSurfaceType>>.Failure($"Failed to retrieve roadsurfacetypes: {ex.Message}");
        }
    }

    public async Task<Result<RoadSurfaceType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RoadSurfaceType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RoadSurfaceType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RoadSurfaceType>.Success(cachedData)
                    : Result<RoadSurfaceType>.Failure("RoadSurfaceType not found");
            }

            var sql = "SELECT * FROM road_surface_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RoadSurfaceType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RoadSurfaceType>.Success(result);
            }
            
            return Result<RoadSurfaceType>.Failure("RoadSurfaceType not found");
        }
        catch (Exception ex)
        {
            return Result<RoadSurfaceType>.Failure($"Failed to retrieve roadsurfacetype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RoadSurfaceType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RoadSurfaceType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadSurfaceType>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadSurfaceType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM road_surface_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RoadSurfaceType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RoadSurfaceType>>.Success(result);
            }
            
            return Result<IEnumerable<RoadSurfaceType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadSurfaceType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RoadSurfaceType>> CreateAsync(RoadSurfaceType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadSurfaceType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO road_surface_type (type)
                VALUES (@Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RoadSurfaceType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadSurfaceType>.Failure($"Failed to create roadsurfacetype: {ex.Message}");
        }
    }

    public async Task<Result<RoadSurfaceType>> UpdateAsync(RoadSurfaceType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadSurfaceType>.Failure(errors);
            }

            var sql = @"
                UPDATE road_surface_type SET

                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RoadSurfaceType>.Failure("RoadSurfaceType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RoadSurfaceType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadSurfaceType>.Failure($"Failed to update roadsurfacetype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM road_surface_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RoadSurfaceType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete roadsurfacetype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RoadSurfaceType? entity = null, int? deletedId = null)
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
