using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RoadSubdivisionService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "roadsubdivisions:";
    private const string HistoryCacheKeyPrefix = "roadsubdivisions:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RoadSubdivisionService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RoadSubdivision>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadSubdivision>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadSubdivision>>.Success(cachedData);
            }

            var sql = "SELECT * FROM road_subdivision";
            var result = await _db.QueryAsync<RoadSubdivision>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RoadSubdivision>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadSubdivision>>.Failure($"Failed to retrieve roadsubdivisions: {ex.Message}");
        }
    }

    public async Task<Result<RoadSubdivision>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RoadSubdivision>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RoadSubdivision>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RoadSubdivision>.Success(cachedData)
                    : Result<RoadSubdivision>.Failure("RoadSubdivision not found");
            }

            var sql = "SELECT * FROM road_subdivision WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RoadSubdivision>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RoadSubdivision>.Success(result);
            }
            
            return Result<RoadSubdivision>.Failure("RoadSubdivision not found");
        }
        catch (Exception ex)
        {
            return Result<RoadSubdivision>.Failure($"Failed to retrieve roadsubdivision: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RoadSubdivision>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RoadSubdivision>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadSubdivision>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadSubdivision>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM road_subdivision 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RoadSubdivision>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RoadSubdivision>>.Success(result);
            }
            
            return Result<IEnumerable<RoadSubdivision>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadSubdivision>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RoadSubdivision>> CreateAsync(RoadSubdivision entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadSubdivision>.Failure(errors);
            }

            var sql = @"
                INSERT INTO road_subdivision (name, feature_object_id)
                VALUES (@Name @FeatureObjectId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RoadSubdivision>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadSubdivision>.Failure($"Failed to create roadsubdivision: {ex.Message}");
        }
    }

    public async Task<Result<RoadSubdivision>> UpdateAsync(RoadSubdivision entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadSubdivision>.Failure(errors);
            }

            var sql = @"
                UPDATE road_subdivision SET
                    name = @Name,
                    feature_object_id = @FeatureObjectId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RoadSubdivision>.Failure("RoadSubdivision not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RoadSubdivision>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadSubdivision>.Failure($"Failed to update roadsubdivision: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM road_subdivision WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RoadSubdivision not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete roadsubdivision: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RoadSubdivision? entity = null, int? deletedId = null)
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
