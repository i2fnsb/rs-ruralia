using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RfqProjectScopeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "rfqprojectscopes:";
    private const string HistoryCacheKeyPrefix = "rfqprojectscopes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RfqProjectScopeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RfqProjectScope>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqProjectScope>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqProjectScope>>.Success(cachedData);
            }

            var sql = "SELECT * FROM rfq_project_scope";
            var result = await _db.QueryAsync<RfqProjectScope>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RfqProjectScope>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqProjectScope>>.Failure($"Failed to retrieve rfqprojectscopes: {ex.Message}");
        }
    }

    public async Task<Result<RfqProjectScope>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RfqProjectScope>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RfqProjectScope>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RfqProjectScope>.Success(cachedData)
                    : Result<RfqProjectScope>.Failure("RfqProjectScope not found");
            }

            var sql = "SELECT * FROM rfq_project_scope WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RfqProjectScope>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RfqProjectScope>.Success(result);
            }
            
            return Result<RfqProjectScope>.Failure("RfqProjectScope not found");
        }
        catch (Exception ex)
        {
            return Result<RfqProjectScope>.Failure($"Failed to retrieve rfqprojectscope: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RfqProjectScope>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RfqProjectScope>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqProjectScope>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqProjectScope>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM rfq_project_scope 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RfqProjectScope>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RfqProjectScope>>.Success(result);
            }
            
            return Result<IEnumerable<RfqProjectScope>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqProjectScope>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RfqProjectScope>> CreateAsync(RfqProjectScope entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqProjectScope>.Failure(errors);
            }

            var sql = @"
                INSERT INTO rfq_project_scope (scope)
                VALUES (@Scope -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RfqProjectScope>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqProjectScope>.Failure($"Failed to create rfqprojectscope: {ex.Message}");
        }
    }

    public async Task<Result<RfqProjectScope>> UpdateAsync(RfqProjectScope entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqProjectScope>.Failure(errors);
            }

            var sql = @"
                UPDATE rfq_project_scope SET

                    scope = @Scope
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RfqProjectScope>.Failure("RfqProjectScope not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RfqProjectScope>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqProjectScope>.Failure($"Failed to update rfqprojectscope: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM rfq_project_scope WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RfqProjectScope not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete rfqprojectscope: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RfqProjectScope? entity = null, int? deletedId = null)
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
