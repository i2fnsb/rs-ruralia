using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class PersonSuffixService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "personsuffixes:";
    private const string HistoryCacheKeyPrefix = "personsuffixes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public PersonSuffixService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<PersonSuffix>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonSuffix>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonSuffix>>.Success(cachedData);
            }

            var sql = "SELECT * FROM person_suffix";
            var result = await _db.QueryAsync<PersonSuffix>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<PersonSuffix>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonSuffix>>.Failure($"Failed to retrieve personsuffixes: {ex.Message}");
        }
    }

    public async Task<Result<PersonSuffix>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<PersonSuffix>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<PersonSuffix>(cached.ToString()!);
                return cachedData != null 
                    ? Result<PersonSuffix>.Success(cachedData)
                    : Result<PersonSuffix>.Failure("PersonSuffix not found");
            }

            var sql = "SELECT * FROM person_suffix WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<PersonSuffix>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<PersonSuffix>.Success(result);
            }
            
            return Result<PersonSuffix>.Failure("PersonSuffix not found");
        }
        catch (Exception ex)
        {
            return Result<PersonSuffix>.Failure($"Failed to retrieve personsuffix: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<PersonSuffix>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<PersonSuffix>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<PersonSuffix>>(cached.ToString()!)!;
                return Result<IEnumerable<PersonSuffix>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM person_suffix 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<PersonSuffix>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<PersonSuffix>>.Success(result);
            }
            
            return Result<IEnumerable<PersonSuffix>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PersonSuffix>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<PersonSuffix>> CreateAsync(PersonSuffix entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonSuffix>.Failure(errors);
            }

            var sql = @"
                INSERT INTO person_suffix (suffix)
                VALUES (@Suffix -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<PersonSuffix>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonSuffix>.Failure($"Failed to create personsuffix: {ex.Message}");
        }
    }

    public async Task<Result<PersonSuffix>> UpdateAsync(PersonSuffix entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<PersonSuffix>.Failure(errors);
            }

            var sql = @"
                UPDATE person_suffix SET

                    suffix = @Suffix
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<PersonSuffix>.Failure("PersonSuffix not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<PersonSuffix>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<PersonSuffix>.Failure($"Failed to update personsuffix: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM person_suffix WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("PersonSuffix not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete personsuffix: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(PersonSuffix? entity = null, int? deletedId = null)
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
