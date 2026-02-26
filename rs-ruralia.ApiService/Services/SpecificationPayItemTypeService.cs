using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class SpecificationPayItemTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "specificationpayitemtypes:";
    private const string HistoryCacheKeyPrefix = "specificationpayitemtypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public SpecificationPayItemTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<SpecificationPayItemType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayItemType>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayItemType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM specification_pay_item_type";
            var result = await _db.QueryAsync<SpecificationPayItemType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<SpecificationPayItemType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayItemType>>.Failure($"Failed to retrieve specificationpayitemtypes: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItemType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<SpecificationPayItemType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<SpecificationPayItemType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<SpecificationPayItemType>.Success(cachedData)
                    : Result<SpecificationPayItemType>.Failure("SpecificationPayItemType not found");
            }

            var sql = "SELECT * FROM specification_pay_item_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<SpecificationPayItemType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<SpecificationPayItemType>.Success(result);
            }
            
            return Result<SpecificationPayItemType>.Failure("SpecificationPayItemType not found");
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItemType>.Failure($"Failed to retrieve specificationpayitemtype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SpecificationPayItemType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<SpecificationPayItemType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayItemType>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayItemType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM specification_pay_item_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<SpecificationPayItemType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<SpecificationPayItemType>>.Success(result);
            }
            
            return Result<IEnumerable<SpecificationPayItemType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayItemType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItemType>> CreateAsync(SpecificationPayItemType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayItemType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO specification_pay_item_type (code, type)
                VALUES (@Code @Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<SpecificationPayItemType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItemType>.Failure($"Failed to create specificationpayitemtype: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItemType>> UpdateAsync(SpecificationPayItemType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayItemType>.Failure(errors);
            }

            var sql = @"
                UPDATE specification_pay_item_type SET
                    code = @Code,
                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<SpecificationPayItemType>.Failure("SpecificationPayItemType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<SpecificationPayItemType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItemType>.Failure($"Failed to update specificationpayitemtype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM specification_pay_item_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("SpecificationPayItemType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete specificationpayitemtype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(SpecificationPayItemType? entity = null, int? deletedId = null)
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
