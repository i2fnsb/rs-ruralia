using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CorrespondenceAddressService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "correspondenceaddresses:";
    private const string HistoryCacheKeyPrefix = "correspondenceaddresses:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CorrespondenceAddressService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CorrespondenceAddress>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceAddress>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceAddress>>.Success(cachedData);
            }

            var sql = "SELECT * FROM correspondence_address";
            var result = await _db.QueryAsync<CorrespondenceAddress>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CorrespondenceAddress>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceAddress>>.Failure($"Failed to retrieve correspondenceaddresses: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddress>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CorrespondenceAddress>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CorrespondenceAddress>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CorrespondenceAddress>.Success(cachedData)
                    : Result<CorrespondenceAddress>.Failure("CorrespondenceAddress not found");
            }

            var sql = "SELECT * FROM correspondence_address WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CorrespondenceAddress>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CorrespondenceAddress>.Success(result);
            }
            
            return Result<CorrespondenceAddress>.Failure("CorrespondenceAddress not found");
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddress>.Failure($"Failed to retrieve correspondenceaddress: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CorrespondenceAddress>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CorrespondenceAddress>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CorrespondenceAddress>>(cached.ToString()!)!;
                return Result<IEnumerable<CorrespondenceAddress>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM correspondence_address 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CorrespondenceAddress>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CorrespondenceAddress>>.Success(result);
            }
            
            return Result<IEnumerable<CorrespondenceAddress>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CorrespondenceAddress>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddress>> CreateAsync(CorrespondenceAddress entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceAddress>.Failure(errors);
            }

            var sql = @"
                INSERT INTO correspondence_address (address, city, state, zip, zip_ext, country, correspondence_address_type_id, person_profile_id, commissioner_profile_id, vendor_profile_id)
                VALUES (@Address @City @State @Zip @ZipExt @Country @CorrespondenceAddressTypeId @PersonProfileId @CommissionerProfileId @VendorProfileId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CorrespondenceAddress>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddress>.Failure($"Failed to create correspondenceaddress: {ex.Message}");
        }
    }

    public async Task<Result<CorrespondenceAddress>> UpdateAsync(CorrespondenceAddress entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CorrespondenceAddress>.Failure(errors);
            }

            var sql = @"
                UPDATE correspondence_address SET
                    address = @Address,                     city = @City,                     state = @State,                     zip = @Zip,                     zip_ext = @ZipExt,                     country = @Country,                     correspondence_address_type_id = @CorrespondenceAddressTypeId,                     person_profile_id = @PersonProfileId,                     commissioner_profile_id = @CommissionerProfileId,
                    vendor_profile_id = @VendorProfileId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CorrespondenceAddress>.Failure("CorrespondenceAddress not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CorrespondenceAddress>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CorrespondenceAddress>.Failure($"Failed to update correspondenceaddress: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM correspondence_address WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CorrespondenceAddress not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete correspondenceaddress: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CorrespondenceAddress? entity = null, int? deletedId = null)
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
