using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class PersonProfileVendorService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "personprofilevendors:";
    private const string HistoryCacheKeyPrefix = "personprofilevendors:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public PersonProfileVendorService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<PersonProfileVendor>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonProfileVendor>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonProfileVendor>>.Success(cachedData);
            }

            var sql = "SELECT * FROM person_profile_vendor";
            var result = await _db.QueryAsync<PersonProfileVendor>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<PersonProfileVendor>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonProfileVendor>>.Failure($"Failed to retrieve personprofilevendors: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfileVendor>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<PersonProfileVendor>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<PersonProfileVendor>(cached.ToString()!);
                return cachedData != null 
                    ? Result<PersonProfileVendor>.Success(cachedData)
                    : Result<PersonProfileVendor>.Failure("PersonProfileVendor not found");
            }

            var sql = "SELECT * FROM person_profile_vendor WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<PersonProfileVendor>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<PersonProfileVendor>.Success(result);
            }
            
            return Result<PersonProfileVendor>.Failure("PersonProfileVendor not found");
        }
        catch (Exception ex)
        {
            return Result<PersonProfileVendor>.Failure($"Failed to retrieve personprofilevendor: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<PersonProfileVendor>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<PersonProfileVendor>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonProfileVendor>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonProfileVendor>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM person_profile_vendor 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<PersonProfileVendor>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<PersonProfileVendor>>.Success(result);
            }
            
            return Result<IEnumerable<PersonProfileVendor>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonProfileVendor>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfileVendor>> CreateAsync(PersonProfileVendor entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonProfileVendor>.Failure(errors);
            }

            var sql = @"
                INSERT INTO person_profile_vendor (title, primary_contact, active, person_profile_id, vendor_profile_id)
                VALUES (@Title @PrimaryContact @Active @PersonProfileId @VendorProfileId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<PersonProfileVendor>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonProfileVendor>.Failure($"Failed to create personprofilevendor: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfileVendor>> UpdateAsync(PersonProfileVendor entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonProfileVendor>.Failure(errors);
            }

            var sql = @"
                UPDATE person_profile_vendor SET
                    title = @Title,                     primary_contact = @PrimaryContact,                     active = @Active,                     person_profile_id = @PersonProfileId,
                    vendor_profile_id = @VendorProfileId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<PersonProfileVendor>.Failure("PersonProfileVendor not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<PersonProfileVendor>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonProfileVendor>.Failure($"Failed to update personprofilevendor: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM person_profile_vendor WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("PersonProfileVendor not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete personprofilevendor: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(PersonProfileVendor? entity = null, int? deletedId = null)
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
