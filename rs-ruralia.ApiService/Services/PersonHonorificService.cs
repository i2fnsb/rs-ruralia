using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class PersonHonorificService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "personhonorifics:";
    private const string HistoryCacheKeyPrefix = "personhonorifics:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public PersonHonorificService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<PersonHonorific>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonHonorific>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonHonorific>>.Success(cachedData);
            }

            var sql = "SELECT * FROM person_honorific";
            var result = await _db.QueryAsync<PersonHonorific>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<PersonHonorific>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonHonorific>>.Failure($"Failed to retrieve personhonorifics: {ex.Message}");
        }
    }

    public async Task<Result<PersonHonorific>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<PersonHonorific>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<PersonHonorific>(cached.ToString()!);
                return cachedData != null 
                    ? Result<PersonHonorific>.Success(cachedData)
                    : Result<PersonHonorific>.Failure("PersonHonorific not found");
            }

            var sql = "SELECT * FROM person_honorific WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<PersonHonorific>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<PersonHonorific>.Success(result);
            }
            
            return Result<PersonHonorific>.Failure("PersonHonorific not found");
        }
        catch (Exception ex)
        {
            return Result<PersonHonorific>.Failure($"Failed to retrieve personhonorific: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<PersonHonorific>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<PersonHonorific>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonHonorific>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonHonorific>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM person_honorific 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<PersonHonorific>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<PersonHonorific>>.Success(result);
            }
            
            return Result<IEnumerable<PersonHonorific>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonHonorific>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<PersonHonorific>> CreateAsync(PersonHonorific entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonHonorific>.Failure(errors);
            }

            var sql = @"
                INSERT INTO person_honorific (honorific)
                VALUES (@Honorific -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<PersonHonorific>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonHonorific>.Failure($"Failed to create personhonorific: {ex.Message}");
        }
    }

    public async Task<Result<PersonHonorific>> UpdateAsync(PersonHonorific entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonHonorific>.Failure(errors);
            }

            var sql = @"
                UPDATE person_honorific SET

                    honorific = @Honorific
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<PersonHonorific>.Failure("PersonHonorific not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<PersonHonorific>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonHonorific>.Failure($"Failed to update personhonorific: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM person_honorific WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("PersonHonorific not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete personhonorific: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(PersonHonorific? entity = null, int? deletedId = null)
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
