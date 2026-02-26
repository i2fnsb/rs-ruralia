using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RoadResponderCodeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "roadrespondercodes:";
    private const string HistoryCacheKeyPrefix = "roadrespondercodes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RoadResponderCodeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RoadResponderCode>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadResponderCode>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadResponderCode>>.Success(cachedData);
            }

            var sql = "SELECT * FROM road_responder_code";
            var result = await _db.QueryAsync<RoadResponderCode>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RoadResponderCode>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadResponderCode>>.Failure($"Failed to retrieve roadrespondercodes: {ex.Message}");
        }
    }

    public async Task<Result<RoadResponderCode>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RoadResponderCode>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RoadResponderCode>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RoadResponderCode>.Success(cachedData)
                    : Result<RoadResponderCode>.Failure("RoadResponderCode not found");
            }

            var sql = "SELECT * FROM road_responder_code WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RoadResponderCode>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RoadResponderCode>.Success(result);
            }
            
            return Result<RoadResponderCode>.Failure("RoadResponderCode not found");
        }
        catch (Exception ex)
        {
            return Result<RoadResponderCode>.Failure($"Failed to retrieve roadrespondercode: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RoadResponderCode>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RoadResponderCode>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RoadResponderCode>>(cached.ToString()!)!;
                return Result<IEnumerable<RoadResponderCode>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM road_responder_code 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RoadResponderCode>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RoadResponderCode>>.Success(result);
            }
            
            return Result<IEnumerable<RoadResponderCode>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoadResponderCode>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RoadResponderCode>> CreateAsync(RoadResponderCode entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadResponderCode>.Failure(errors);
            }

            var sql = @"
                INSERT INTO road_responder_code (code)
                VALUES (@Code -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RoadResponderCode>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadResponderCode>.Failure($"Failed to create roadrespondercode: {ex.Message}");
        }
    }

    public async Task<Result<RoadResponderCode>> UpdateAsync(RoadResponderCode entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RoadResponderCode>.Failure(errors);
            }

            var sql = @"
                UPDATE road_responder_code SET

                    code = @Code
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RoadResponderCode>.Failure("RoadResponderCode not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RoadResponderCode>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RoadResponderCode>.Failure($"Failed to update roadrespondercode: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM road_responder_code WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RoadResponderCode not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete roadrespondercode: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RoadResponderCode? entity = null, int? deletedId = null)
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
