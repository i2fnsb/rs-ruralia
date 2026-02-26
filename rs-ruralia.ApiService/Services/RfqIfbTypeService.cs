using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RfqIfbTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "rfqifbtypes:";
    private const string HistoryCacheKeyPrefix = "rfqifbtypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RfqIfbTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RfqIfbType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqIfbType>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqIfbType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM rfq_ifb_type";
            var result = await _db.QueryAsync<RfqIfbType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RfqIfbType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqIfbType>>.Failure($"Failed to retrieve rfqifbtypes: {ex.Message}");
        }
    }

    public async Task<Result<RfqIfbType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RfqIfbType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RfqIfbType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RfqIfbType>.Success(cachedData)
                    : Result<RfqIfbType>.Failure("RfqIfbType not found");
            }

            var sql = "SELECT * FROM rfq_ifb_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RfqIfbType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RfqIfbType>.Success(result);
            }
            
            return Result<RfqIfbType>.Failure("RfqIfbType not found");
        }
        catch (Exception ex)
        {
            return Result<RfqIfbType>.Failure($"Failed to retrieve rfqifbtype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RfqIfbType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RfqIfbType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqIfbType>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqIfbType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM rfq_ifb_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RfqIfbType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RfqIfbType>>.Success(result);
            }
            
            return Result<IEnumerable<RfqIfbType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqIfbType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RfqIfbType>> CreateAsync(RfqIfbType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqIfbType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO rfq_ifb_type (type)
                VALUES (@Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RfqIfbType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqIfbType>.Failure($"Failed to create rfqifbtype: {ex.Message}");
        }
    }

    public async Task<Result<RfqIfbType>> UpdateAsync(RfqIfbType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqIfbType>.Failure(errors);
            }

            var sql = @"
                UPDATE rfq_ifb_type SET

                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RfqIfbType>.Failure("RfqIfbType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RfqIfbType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqIfbType>.Failure($"Failed to update rfqifbtype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM rfq_ifb_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RfqIfbType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete rfqifbtype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RfqIfbType? entity = null, int? deletedId = null)
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
