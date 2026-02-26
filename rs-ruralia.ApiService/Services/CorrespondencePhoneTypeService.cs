using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondencePhoneTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondencephonetypes:";
    private const string HistoryCacheKeyPrefix = "correspondencephonetypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondencePhoneTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondencePhoneType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondencePhoneType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondencePhoneType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_phone_type";
            var result = await _db.QueryAsync<CorrespondencePhoneType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondencePhoneType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondencePhoneType>>.Failure($"Failed to retrieve correspondencephonetypes: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhoneType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondencePhoneType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondencePhoneType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondencePhoneType>.Success(cachedData)
                    : Result<CorrespondencePhoneType>.Failure("CorrespondencePhoneType not found");
            }

            var sql = "SELECT * FROM correspondence_phone_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondencePhoneType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondencePhoneType>.Success(result);
            }
            
            return Result<CorrespondencePhoneType>.Failure("CorrespondencePhoneType not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhoneType>.Failure($"Failed to retrieve correspondencephonetype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondencePhoneType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondencePhoneType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondencePhoneType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondencePhoneType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_phone_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondencePhoneType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondencePhoneType>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondencePhoneType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondencePhoneType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhoneType>> CreateAsync(CorrespondencePhoneType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondencePhoneType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_phone_type (type, description)
                VALUES (@Type @Description -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondencePhoneType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhoneType>.Failure($"Failed to create correspondencephonetype: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhoneType>> UpdateAsync(CorrespondencePhoneType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondencePhoneType>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_phone_type SET
                    type = @Type,
                    description = @Description
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondencePhoneType>.Failure("CorrespondencePhoneType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondencePhoneType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhoneType>.Failure($"Failed to update correspondencephonetype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_phone_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondencePhoneType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondencephonetype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondencePhoneType? entity = null, int? deletedId = null)
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
