using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondenceEmailService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondenceemails:";
    private const string HistoryCacheKeyPrefix = "correspondenceemails:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondenceEmailService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondenceEmail>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceEmail>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceEmail>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_email";
            var result = await _db.QueryAsync<CorrespondenceEmail>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondenceEmail>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceEmail>>.Failure($"Failed to retrieve correspondenceemails: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmail>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondenceEmail>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondenceEmail>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondenceEmail>.Success(cachedData)
                    : Result<CorrespondenceEmail>.Failure("CorrespondenceEmail not found");
            }

            var sql = "SELECT * FROM correspondence_email WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondenceEmail>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondenceEmail>.Success(result);
            }
            
            return Result<CorrespondenceEmail>.Failure("CorrespondenceEmail not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmail>.Failure($"Failed to retrieve correspondenceemail: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondenceEmail>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondenceEmail>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceEmail>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceEmail>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_email 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondenceEmail>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondenceEmail>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondenceEmail>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceEmail>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmail>> CreateAsync(CorrespondenceEmail entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceEmail>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_email (email, correspondence_email_type_id, person_profile_id, commissioner_profile_id, vendor_profile_id)
                VALUES (@Email @CorrespondenceEmailTypeId @PersonProfileId @CommissionerProfileId @VendorProfileId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondenceEmail>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmail>.Failure($"Failed to create correspondenceemail: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceEmail>> UpdateAsync(CorrespondenceEmail entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceEmail>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_email SET
                    email = @Email,                     correspondence_email_type_id = @CorrespondenceEmailTypeId,                     person_profile_id = @PersonProfileId,                     commissioner_profile_id = @CommissionerProfileId,
                    vendor_profile_id = @VendorProfileId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondenceEmail>.Failure("CorrespondenceEmail not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondenceEmail>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceEmail>.Failure($"Failed to update correspondenceemail: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_email WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondenceEmail not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondenceemail: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondenceEmail? entity = null, int? deletedId = null)
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
