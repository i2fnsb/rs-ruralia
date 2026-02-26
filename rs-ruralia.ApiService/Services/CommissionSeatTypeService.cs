using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CommissionSeatTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "commissionseattype:";
    private const string HistoryCacheKeyPrefix = "commissionseattype:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CommissionSeatTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CommissionSeatType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatType>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commission_seat_type ORDER BY type";
            var result = await _db.QueryAsync<CommissionSeatType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionSeatType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatType>>.Failure($"Failed to retrieve commission seat types: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CommissionSeatType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CommissionSeatType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CommissionSeatType>.Success(cachedData)
                    : Result<CommissionSeatType>.Failure("Commission seat type not found");
            }

            var sql = "SELECT * FROM commission_seat_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CommissionSeatType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CommissionSeatType>.Success(result);
            }
            
            return Result<CommissionSeatType>.Failure("Commission seat type not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Failed to retrieve commission seat type: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeatType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CommissionSeatType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeatType>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeatType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM commission_seat_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CommissionSeatType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CommissionSeatType>>.Success(result);
            }
            
            return Result<IEnumerable<CommissionSeatType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeatType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> CreateAsync(CommissionSeatType commissionSeatType)
    {
        try
        {
            if (!ModelValidator.TryValidate(commissionSeatType, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO commission_seat_type (type, type_code)
                VALUES (@Type, @TypeCode);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            commissionSeatType.Id = await _db.ExecuteScalarAsync<int>(sql, commissionSeatType);
            await InvalidateCacheAsync();
            
            return Result<CommissionSeatType>.Success(commissionSeatType);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Failed to create commission seat type: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeatType>> UpdateAsync(CommissionSeatType commissionSeatType)
    {
        try
        {
            if (!ModelValidator.TryValidate(commissionSeatType, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeatType>.Failure(errors);
            }

            var sql = @"
                UPDATE commission_seat_type SET
                    type = @Type,
                    type_code = @TypeCode
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, commissionSeatType);
            
            if (affected == 0)
                return Result<CommissionSeatType>.Failure("Commission seat type not found or no changes made");

            await InvalidateCacheAsync(commissionSeatType);
            return Result<CommissionSeatType>.Success(commissionSeatType);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeatType>.Failure($"Failed to update commission seat type: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM commission_seat_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("Commission seat type not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete commission seat type: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CommissionSeatType? commissionSeatType = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");
        
        if (commissionSeatType != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{commissionSeatType.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{commissionSeatType.Id}");
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}