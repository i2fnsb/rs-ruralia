using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class SpecificationPayItemService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "specificationpayitems:";
    private const string HistoryCacheKeyPrefix = "specificationpayitems:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public SpecificationPayItemService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<SpecificationPayItem>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayItem>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayItem>>.Success(cachedData);
            }

            var sql = "SELECT * FROM specification_pay_item";
            var result = await _db.QueryAsync<SpecificationPayItem>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<SpecificationPayItem>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayItem>>.Failure($"Failed to retrieve specificationpayitems: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItem>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<SpecificationPayItem>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<SpecificationPayItem>(cached.ToString()!);
                return cachedData != null 
                    ? Result<SpecificationPayItem>.Success(cachedData)
                    : Result<SpecificationPayItem>.Failure("SpecificationPayItem not found");
            }

            var sql = "SELECT * FROM specification_pay_item WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<SpecificationPayItem>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<SpecificationPayItem>.Success(result);
            }
            
            return Result<SpecificationPayItem>.Failure("SpecificationPayItem not found");
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItem>.Failure($"Failed to retrieve specificationpayitem: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<SpecificationPayItem>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<SpecificationPayItem>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<SpecificationPayItem>>(cached.ToString()!)!;
                return Result<IEnumerable<SpecificationPayItem>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM specification_pay_item 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<SpecificationPayItem>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<SpecificationPayItem>>.Success(result);
            }
            
            return Result<IEnumerable<SpecificationPayItem>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<SpecificationPayItem>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItem>> CreateAsync(SpecificationPayItem entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayItem>.Failure(errors);
            }

            var sql = @"
                INSERT INTO specification_pay_item (pay_item_number, pay_item, pay_item_2, original_published, special_conditions, conditions_description, specification_pay_item_type_id, specification_pay_unit_type_id)
                VALUES (@PayItemNumber @PayItem @PayItem2 @OriginalPublished @SpecialConditions @ConditionsDescription @SpecificationPayItemTypeId @SpecificationPayUnitTypeId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<SpecificationPayItem>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItem>.Failure($"Failed to create specificationpayitem: {ex.Message}");
        }
    }

    public async Task<Result<SpecificationPayItem>> UpdateAsync(SpecificationPayItem entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<SpecificationPayItem>.Failure(errors);
            }

            var sql = @"
                UPDATE specification_pay_item SET
                    pay_item_number = @PayItemNumber,                     pay_item = @PayItem,                     pay_item_2 = @PayItem2,                     original_published = @OriginalPublished,                     special_conditions = @SpecialConditions,                     conditions_description = @ConditionsDescription,                     specification_pay_item_type_id = @SpecificationPayItemTypeId,
                    specification_pay_unit_type_id = @SpecificationPayUnitTypeId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<SpecificationPayItem>.Failure("SpecificationPayItem not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<SpecificationPayItem>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<SpecificationPayItem>.Failure($"Failed to update specificationpayitem: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM specification_pay_item WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("SpecificationPayItem not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete specificationpayitem: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(SpecificationPayItem? entity = null, int? deletedId = null)
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
