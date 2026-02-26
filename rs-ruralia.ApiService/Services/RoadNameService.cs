using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RoadNameService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "roadnames:";
    private const string HistoryCacheKeyPrefix = "roadnames:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RoadNameService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RoadName>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadName>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadName>>.Success(cachedData);
            }

            var sql = "SELECT * FROM road_name";
            var result = await _db.QueryAsync<RoadName>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RoadName>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadName>>.Failure($"Failed to retrieve roadnames: {ex.Message}");
        }
    }

    public async Task<Result<RoadName>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RoadName>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RoadName>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RoadName>.Success(cachedData)
                    : Result<RoadName>.Failure("RoadName not found");
            }

            var sql = "SELECT * FROM road_name WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RoadName>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RoadName>.Success(result);
            }
            
            return Result<RoadName>.Failure("RoadName not found");
        }
        catch (Exception ex)
        {
            return Result<RoadName>.Failure($"Failed to retrieve roadname: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RoadName>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RoadName>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadName>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadName>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM road_name 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RoadName>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RoadName>>.Success(result);
            }
            
            return Result<IEnumerable<RoadName>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadName>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RoadName>> CreateAsync(RoadName entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadName>.Failure(errors);
            }

            var sql = @"
                INSERT INTO road_name (name, match_feature_layer)
                VALUES (@Name @MatchFeatureLayer -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RoadName>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadName>.Failure($"Failed to create roadname: {ex.Message}");
        }
    }

    public async Task<Result<RoadName>> UpdateAsync(RoadName entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadName>.Failure(errors);
            }

            var sql = @"
                UPDATE road_name SET
                    name = @Name,
                    match_feature_layer = @MatchFeatureLayer
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RoadName>.Failure("RoadName not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RoadName>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadName>.Failure($"Failed to update roadname: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM road_name WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RoadName not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete roadname: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RoadName? entity = null, int? deletedId = null)
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
