using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class SpecificationPayUnitTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "specificationpayunittypes:";
    private const string HistoryCacheKeyPrefix = "specificationpayunittypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public SpecificationPayUnitTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<SpecificationPayUnitType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayUnitType>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayUnitType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM specification_pay_unit_type";
            var result = await _db.QueryAsync<SpecificationPayUnitType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<SpecificationPayUnitType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayUnitType>>.Failure($"Failed to retrieve specificationpayunittypes: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayUnitType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<SpecificationPayUnitType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<SpecificationPayUnitType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<SpecificationPayUnitType>.Success(cachedData)
                    : Result<SpecificationPayUnitType>.Failure("SpecificationPayUnitType not found");
            }

            var sql = "SELECT * FROM specification_pay_unit_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<SpecificationPayUnitType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<SpecificationPayUnitType>.Success(result);
            }
            
            return Result<SpecificationPayUnitType>.Failure("SpecificationPayUnitType not found");
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayUnitType>.Failure($"Failed to retrieve specificationpayunittype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SpecificationPayUnitType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<SpecificationPayUnitType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayUnitType>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayUnitType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM specification_pay_unit_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<SpecificationPayUnitType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<SpecificationPayUnitType>>.Success(result);
            }
            
            return Result<IEnumerable<SpecificationPayUnitType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayUnitType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayUnitType>> CreateAsync(SpecificationPayUnitType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayUnitType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO specification_pay_unit_type (unit, description)
                VALUES (@Unit @Description -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<SpecificationPayUnitType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayUnitType>.Failure($"Failed to create specificationpayunittype: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayUnitType>> UpdateAsync(SpecificationPayUnitType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayUnitType>.Failure(errors);
            }

            var sql = @"
                UPDATE specification_pay_unit_type SET
                    unit = @Unit,
                    description = @Description
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<SpecificationPayUnitType>.Failure("SpecificationPayUnitType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<SpecificationPayUnitType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayUnitType>.Failure($"Failed to update specificationpayunittype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM specification_pay_unit_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("SpecificationPayUnitType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete specificationpayunittype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(SpecificationPayUnitType? entity = null, int? deletedId = null)
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
