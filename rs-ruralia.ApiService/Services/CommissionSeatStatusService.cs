using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CommissionSeatStatusService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "commissionseatstatuses:";
    private const string HistoryCacheKeyPrefix = "commissionseatstatuses:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CommissionSeatStatusService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CommissionSeatStatus>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatStatus>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatStatus>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commission_seat_status";
            var result = await _db.QueryAsync<CommissionSeatStatus>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionSeatStatus>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatStatus>>.Failure($"Failed to retrieve commissionseatstatuses: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatStatus>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CommissionSeatStatus>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CommissionSeatStatus>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CommissionSeatStatus>.Success(cachedData)
                    : Result<CommissionSeatStatus>.Failure("CommissionSeatStatus not found");
            }

            var sql = "SELECT * FROM commission_seat_status WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CommissionSeatStatus>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CommissionSeatStatus>.Success(result);
            }
            
            return Result<CommissionSeatStatus>.Failure("CommissionSeatStatus not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatStatus>.Failure($"Failed to retrieve commissionseatstatus: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeatStatus>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CommissionSeatStatus>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatStatus>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatStatus>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM commission_seat_status 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CommissionSeatStatus>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CommissionSeatStatus>>.Success(result);
            }
            
            return Result<IEnumerable<CommissionSeatStatus>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatStatus>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatStatus>> CreateAsync(CommissionSeatStatus entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatStatus>.Failure(errors);
            }

            var sql = @"
                INSERT INTO commission_seat_status (status, status_code, description, active)
                VALUES (@Status @StatusCode @Description @Active -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CommissionSeatStatus>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatStatus>.Failure($"Failed to create commissionseatstatus: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatStatus>> UpdateAsync(CommissionSeatStatus entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatStatus>.Failure(errors);
            }

            var sql = @"
                UPDATE commission_seat_status SET
                    status = @Status,                     status_code = @StatusCode,                     description = @Description,
                    active = @Active
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CommissionSeatStatus>.Failure("CommissionSeatStatus not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CommissionSeatStatus>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatStatus>.Failure($"Failed to update commissionseatstatus: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM commission_seat_status WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CommissionSeatStatus not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete commissionseatstatus: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CommissionSeatStatus? entity = null, int? deletedId = null)
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
