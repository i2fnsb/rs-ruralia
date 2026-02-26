using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CommissionSeatClassService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "commissionseatclasses:";
    private const string HistoryCacheKeyPrefix = "commissionseatclasses:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CommissionSeatClassService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CommissionSeatClass>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatClass>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatClass>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commission_seat_class";
            var result = await _db.QueryAsync<CommissionSeatClass>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionSeatClass>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatClass>>.Failure($"Failed to retrieve commissionseatclasses: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatClass>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CommissionSeatClass>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CommissionSeatClass>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CommissionSeatClass>.Success(cachedData)
                    : Result<CommissionSeatClass>.Failure("CommissionSeatClass not found");
            }

            var sql = "SELECT * FROM commission_seat_class WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CommissionSeatClass>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CommissionSeatClass>.Success(result);
            }
            
            return Result<CommissionSeatClass>.Failure("CommissionSeatClass not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatClass>.Failure($"Failed to retrieve commissionseatclass: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeatClass>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CommissionSeatClass>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatClass>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatClass>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM commission_seat_class 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CommissionSeatClass>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CommissionSeatClass>>.Success(result);
            }
            
            return Result<IEnumerable<CommissionSeatClass>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatClass>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatClass>> CreateAsync(CommissionSeatClass entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatClass>.Failure(errors);
            }

            var sql = @"
                INSERT INTO commission_seat_class (class, description)
                VALUES (@Class @Description -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CommissionSeatClass>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatClass>.Failure($"Failed to create commissionseatclass: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatClass>> UpdateAsync(CommissionSeatClass entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatClass>.Failure(errors);
            }

            var sql = @"
                UPDATE commission_seat_class SET
                    class = @Class,
                    description = @Description
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CommissionSeatClass>.Failure("CommissionSeatClass not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CommissionSeatClass>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatClass>.Failure($"Failed to update commissionseatclass: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM commission_seat_class WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CommissionSeatClass not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete commissionseatclass: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CommissionSeatClass? entity = null, int? deletedId = null)
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
