using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondenceEmailTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondenceemailtypes:";
    private const string HistoryCacheKeyPrefix = "correspondenceemailtypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondenceEmailTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondenceEmailType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceEmailType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceEmailType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_email_type";
            var result = await _db.QueryAsync<CorrespondenceEmailType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondenceEmailType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceEmailType>>.Failure($"Failed to retrieve correspondenceemailtypes: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmailType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondenceEmailType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondenceEmailType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondenceEmailType>.Success(cachedData)
                    : Result<CorrespondenceEmailType>.Failure("CorrespondenceEmailType not found");
            }

            var sql = "SELECT * FROM correspondence_email_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondenceEmailType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondenceEmailType>.Success(result);
            }
            
            return Result<CorrespondenceEmailType>.Failure("CorrespondenceEmailType not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmailType>.Failure($"Failed to retrieve correspondenceemailtype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondenceEmailType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondenceEmailType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceEmailType>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceEmailType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_email_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondenceEmailType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondenceEmailType>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondenceEmailType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceEmailType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmailType>> CreateAsync(CorrespondenceEmailType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceEmailType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_email_type (type)
                VALUES (@Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondenceEmailType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmailType>.Failure($"Failed to create correspondenceemailtype: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmailType>> UpdateAsync(CorrespondenceEmailType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceEmailType>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_email_type SET

                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondenceEmailType>.Failure("CorrespondenceEmailType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondenceEmailType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmailType>.Failure($"Failed to update correspondenceemailtype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_email_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondenceEmailType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondenceemailtype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondenceEmailType? entity = null, int? deletedId = null)
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
