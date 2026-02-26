using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondenceProfileService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondenceprofiles:";
    private const string HistoryCacheKeyPrefix = "correspondenceprofiles:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondenceProfileService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondenceProfile>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceProfile>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_profile";
            var result = await _db.QueryAsync<CorrespondenceProfile>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondenceProfile>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceProfile>>.Failure($"Failed to retrieve correspondenceprofiles: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceProfile>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondenceProfile>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondenceProfile>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondenceProfile>.Success(cachedData)
                    : Result<CorrespondenceProfile>.Failure("CorrespondenceProfile not found");
            }

            var sql = "SELECT * FROM correspondence_profile WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondenceProfile>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondenceProfile>.Success(result);
            }
            
            return Result<CorrespondenceProfile>.Failure("CorrespondenceProfile not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceProfile>.Failure($"Failed to retrieve correspondenceprofile: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondenceProfile>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondenceProfile>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceProfile>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_profile 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondenceProfile>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondenceProfile>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondenceProfile>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceProfile>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceProfile>> CreateAsync(CorrespondenceProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceProfile>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_profile (phone, email, fax, mailing_address, public_profile, person_profile_id, commissioner_profile_id, vendor_profile_id, correspondence_phone_id, correspondence_email_id, correspondence_address_id, correspondence_fax_id)
                VALUES (@Phone @Email @Fax @MailingAddress @PublicProfile @PersonProfileId @CommissionerProfileId @VendorProfileId @CorrespondencePhoneId @CorrespondenceEmailId @CorrespondenceAddressId @CorrespondenceFaxId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondenceProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceProfile>.Failure($"Failed to create correspondenceprofile: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceProfile>> UpdateAsync(CorrespondenceProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceProfile>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_profile SET
                    phone = @Phone,                     email = @Email,                     fax = @Fax,                     mailing_address = @MailingAddress,                     public_profile = @PublicProfile,                     person_profile_id = @PersonProfileId,                     commissioner_profile_id = @CommissionerProfileId,                     vendor_profile_id = @VendorProfileId,                     correspondence_phone_id = @CorrespondencePhoneId,                     correspondence_email_id = @CorrespondenceEmailId,                     correspondence_address_id = @CorrespondenceAddressId,
                    correspondence_fax_id = @CorrespondenceFaxId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondenceProfile>.Failure("CorrespondenceProfile not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondenceProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceProfile>.Failure($"Failed to update correspondenceprofile: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_profile WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondenceProfile not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondenceprofile: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondenceProfile? entity = null, int? deletedId = null)
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
