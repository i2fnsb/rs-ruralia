using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class PersonProfileService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "personprofiles:";
    private const string HistoryCacheKeyPrefix = "personprofiles:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public PersonProfileService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<PersonProfile>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonProfile>>.Success(cachedData);
            }

            var sql = "SELECT * FROM person_profile";
            var result = await _db.QueryAsync<PersonProfile>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<PersonProfile>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonProfile>>.Failure($"Failed to retrieve personprofiles: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfile>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<PersonProfile>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<PersonProfile>(cached.ToString()!);
                return cachedData != null 
                    ? Result<PersonProfile>.Success(cachedData)
                    : Result<PersonProfile>.Failure("PersonProfile not found");
            }

            var sql = "SELECT * FROM person_profile WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<PersonProfile>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<PersonProfile>.Success(result);
            }
            
            return Result<PersonProfile>.Failure("PersonProfile not found");
        }
        catch (Exception ex)
        {
            return Result<PersonProfile>.Failure($"Failed to retrieve personprofile: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<PersonProfile>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<PersonProfile>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonProfile>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM person_profile 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<PersonProfile>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<PersonProfile>>.Success(result);
            }
            
            return Result<IEnumerable<PersonProfile>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonProfile>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfile>> CreateAsync(PersonProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonProfile>.Failure(errors);
            }

            var sql = @"
                INSERT INTO person_profile (active, first_name, last_name, middle_name, preferred_name, person_honorific_id, person_suffix_id, auth_id)
                VALUES (@Active @FirstName @LastName @MiddleName @PreferredName @PersonHonorificId @PersonSuffixId @AuthId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<PersonProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonProfile>.Failure($"Failed to create personprofile: {ex.Message}");
        }
    }

    public async Task<Result<PersonProfile>> UpdateAsync(PersonProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonProfile>.Failure(errors);
            }

            var sql = @"
                UPDATE person_profile SET
                    active = @Active,                     first_name = @FirstName,                     last_name = @LastName,                     middle_name = @MiddleName,                     preferred_name = @PreferredName,                     person_honorific_id = @PersonHonorificId,                     person_suffix_id = @PersonSuffixId,
                    auth_id = @AuthId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<PersonProfile>.Failure("PersonProfile not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<PersonProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonProfile>.Failure($"Failed to update personprofile: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM person_profile WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("PersonProfile not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete personprofile: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(PersonProfile? entity = null, int? deletedId = null)
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
