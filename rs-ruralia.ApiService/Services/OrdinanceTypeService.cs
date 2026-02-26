using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class OrdinanceTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "ordinancetype:";
    private const string HistoryCacheKeyPrefix = "ordinancetype:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public OrdinanceTypeService(
        IDbConnection db,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<OrdinanceType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<OrdinanceType>>(cached.ToString()!)!;
                return Result<IEnumerable<OrdinanceType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM ordinance_type ORDER BY description";
            var result = await _db.QueryAsync<OrdinanceType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);

            return Result<IEnumerable<OrdinanceType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<OrdinanceType>>.Failure($"Failed to retrieve ordinance types: {ex.Message}");
        }
    }

    public async Task<Result<OrdinanceType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<OrdinanceType>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<OrdinanceType>(cached.ToString()!);
                return cachedData != null
                    ? Result<OrdinanceType>.Success(cachedData)
                    : Result<OrdinanceType>.Failure("Ordinance type not found");
            }

            var sql = "SELECT * FROM ordinance_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<OrdinanceType>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<OrdinanceType>.Success(result);
            }

            return Result<OrdinanceType>.Failure("Ordinance type not found");
        }
        catch (Exception ex)
        {
            return Result<OrdinanceType>.Failure($"Failed to retrieve ordinance type: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<OrdinanceType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<IEnumerable<OrdinanceType>>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<OrdinanceType>>(cached.ToString()!)!;
                return Result<IEnumerable<OrdinanceType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM ordinance_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";

            var result = await _db.QueryAsync<OrdinanceType>(sql, new { Id = id });

            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<OrdinanceType>>.Success(result);
            }

            return Result<IEnumerable<OrdinanceType>>.Failure("No history found for ordinance type");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<OrdinanceType>>.Failure($"Failed to retrieve ordinance type history: {ex.Message}");
        }
    }

    public async Task<Result<OrdinanceType>> CreateAsync(OrdinanceType ordinanceType)
    {
        try
        {
            if (!ModelValidator.TryValidate(ordinanceType, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<OrdinanceType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO ordinance_type 
                (type, description)
                VALUES 
                (@Type, @Description);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            ordinanceType.Id = await _db.ExecuteScalarAsync<int>(sql, ordinanceType);
            await InvalidateCacheAsync();

            return Result<OrdinanceType>.Success(ordinanceType);
        }
        catch (ValidationException vex)
        {
            return Result<OrdinanceType>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<OrdinanceType>.Failure($"Failed to create ordinance type: {ex.Message}");
        }
    }

    public async Task<Result<OrdinanceType>> UpdateAsync(OrdinanceType ordinanceType)
    {
        try
        {
            if (!ModelValidator.TryValidate(ordinanceType, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<OrdinanceType>.Failure(errors);
            }

            var sql = @"
                UPDATE ordinance_type SET
                    type = @Type,
                    description = @Description
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, ordinanceType);

            if (affected == 0)
            {
                return Result<OrdinanceType>.Failure("Ordinance type not found or no changes made");
            }

            await InvalidateCacheAsync(ordinanceType);

            return Result<OrdinanceType>.Success(ordinanceType);
        }
        catch (ValidationException vex)
        {
            return Result<OrdinanceType>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<OrdinanceType>.Failure($"Failed to update ordinance type: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result.Failure("Invalid ID provided");
            }

            var sql = "DELETE FROM ordinance_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });

            if (affected == 0)
            {
                return Result.Failure("Ordinance type not found");
            }

            await InvalidateCacheAsync(deletedId: id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete ordinance type: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(OrdinanceType? ordinanceType = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");

        if (ordinanceType != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{ordinanceType.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{ordinanceType.Id}");
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}