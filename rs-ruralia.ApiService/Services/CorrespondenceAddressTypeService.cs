using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondenceAddressTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondenceaddresstypes:";
    private const string HistoryCacheKeyPrefix = "correspondenceaddresstypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondenceAddressTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondenceAddressType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceAddressType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceAddressType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_address_type";
            var result = await _db.QueryAsync<CorrespondenceAddressType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondenceAddressType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceAddressType>>.Failure($"Failed to retrieve correspondenceaddresstypes: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddressType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondenceAddressType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondenceAddressType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondenceAddressType>.Success(cachedData)
                    : Result<CorrespondenceAddressType>.Failure("CorrespondenceAddressType not found");
            }

            var sql = "SELECT * FROM correspondence_address_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondenceAddressType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondenceAddressType>.Success(result);
            }
            
            return Result<CorrespondenceAddressType>.Failure("CorrespondenceAddressType not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddressType>.Failure($"Failed to retrieve correspondenceaddresstype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondenceAddressType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondenceAddressType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceAddressType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceAddressType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_address_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondenceAddressType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondenceAddressType>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondenceAddressType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceAddressType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddressType>> CreateAsync(CorrespondenceAddressType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceAddressType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_address_type (type)
                VALUES (@Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondenceAddressType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddressType>.Failure($"Failed to create correspondenceaddresstype: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddressType>> UpdateAsync(CorrespondenceAddressType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceAddressType>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_address_type SET

                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondenceAddressType>.Failure("CorrespondenceAddressType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondenceAddressType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddressType>.Failure($"Failed to update correspondenceaddresstype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_address_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondenceAddressType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondenceaddresstype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondenceAddressType? entity = null, int? deletedId = null)
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
