using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondencePhoneService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondencephones:";
    private const string HistoryCacheKeyPrefix = "correspondencephones:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondencePhoneService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondencePhone>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondencePhone>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondencePhone>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_phone";
            var result = await _db.QueryAsync<CorrespondencePhone>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondencePhone>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondencePhone>>.Failure($"Failed to retrieve correspondencephones: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhone>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondencePhone>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondencePhone>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondencePhone>.Success(cachedData)
                    : Result<CorrespondencePhone>.Failure("CorrespondencePhone not found");
            }

            var sql = "SELECT * FROM correspondence_phone WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondencePhone>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondencePhone>.Success(result);
            }
            
            return Result<CorrespondencePhone>.Failure("CorrespondencePhone not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhone>.Failure($"Failed to retrieve correspondencephone: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondencePhone>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondencePhone>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondencePhone>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondencePhone>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_phone 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondencePhone>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondencePhone>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondencePhone>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondencePhone>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhone>> CreateAsync(CorrespondencePhone entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondencePhone>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_phone (number, correspondence_phone_type_id, person_profile_id, commissioner_profile_id, vendor_profile_id)
                VALUES (@Number @CorrespondencePhoneTypeId @PersonProfileId @CommissionerProfileId @VendorProfileId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondencePhone>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhone>.Failure($"Failed to create correspondencephone: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondencePhone>> UpdateAsync(CorrespondencePhone entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondencePhone>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_phone SET
                    number = @Number,                     correspondence_phone_type_id = @CorrespondencePhoneTypeId,                     person_profile_id = @PersonProfileId,                     commissioner_profile_id = @CommissionerProfileId,
                    vendor_profile_id = @VendorProfileId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondencePhone>.Failure("CorrespondencePhone not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondencePhone>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondencePhone>.Failure($"Failed to update correspondencephone: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_phone WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondencePhone not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondencephone: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondencePhone? entity = null, int? deletedId = null)
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
