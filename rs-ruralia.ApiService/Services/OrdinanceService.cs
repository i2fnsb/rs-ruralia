using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class OrdinanceService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "ordinance:";
    private const string HistoryCacheKeyPrefix = "ordinance:history:";
    private const string ServiceAreaCacheKeyPrefix = "ordinance:servicearea:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public OrdinanceService(
        IDbConnection db,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            var sql = "SELECT * FROM ordinance ORDER BY ordinance_year DESC, ordinance";
            var result = await _db.QueryAsync<Ordinance>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);

            return Result<IEnumerable<Ordinance>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinances: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<Ordinance>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<Ordinance>(cached.ToString()!);
                return cachedData != null
                    ? Result<Ordinance>.Success(cachedData)
                    : Result<Ordinance>.Failure("Ordinance not found");
            }

            var sql = "SELECT * FROM ordinance WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Ordinance>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<Ordinance>.Success(result);
            }

            return Result<Ordinance>.Failure("Ordinance not found");
        }
        catch (Exception ex)
        {
            return Result<Ordinance>.Failure($"Failed to retrieve ordinance: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetByServiceAreaIdAsync(int serviceAreaId)
    {
        try
        {
            if (serviceAreaId <= 0)
            {
                return Result<IEnumerable<Ordinance>>.Failure("Invalid Service Area ID provided");
            }

            var cacheKey = $"{ServiceAreaCacheKeyPrefix}{serviceAreaId}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            var sql = "SELECT * FROM ordinance WHERE service_area_id = @ServiceAreaId ORDER BY ordinance_year DESC, ordinance";
            var result = await _db.QueryAsync<Ordinance>(sql, new { ServiceAreaId = serviceAreaId });
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);

            return Result<IEnumerable<Ordinance>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinances for service area: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<IEnumerable<Ordinance>>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM ordinance 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";

            var result = await _db.QueryAsync<Ordinance>(sql, new { Id = id });

            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<Ordinance>>.Success(result);
            }

            return Result<IEnumerable<Ordinance>>.Failure("No history found for ordinance");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinance history: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> CreateAsync(Ordinance ordinance)
    {
        try
        {
            if (!ModelValidator.TryValidate(ordinance, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Ordinance>.Failure(errors);
            }

            var sql = @"
                INSERT INTO ordinance 
                (ordinance, ordinance_year, ordinance_passed, ordinance_type_id, service_area_id)
                VALUES 
                (@OrdinanceName, @OrdinanceYear, @OrdinancePassed, @OrdinanceTypeId, @ServiceAreaId);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            ordinance.Id = await _db.ExecuteScalarAsync<int>(sql, ordinance);
            await InvalidateCacheAsync(ordinance);

            return Result<Ordinance>.Success(ordinance);
        }
        catch (ValidationException vex)
        {
            return Result<Ordinance>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<Ordinance>.Failure($"Failed to create ordinance: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> UpdateAsync(Ordinance ordinance)
    {
        try
        {
            if (!ModelValidator.TryValidate(ordinance, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Ordinance>.Failure(errors);
            }

            var sql = @"
                UPDATE ordinance SET
                    ordinance = @OrdinanceName,
                    ordinance_year = @OrdinanceYear,
                    ordinance_passed = @OrdinancePassed,
                    ordinance_type_id = @OrdinanceTypeId,
                    service_area_id = @ServiceAreaId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, ordinance);

            if (affected == 0)
            {
                return Result<Ordinance>.Failure("Ordinance not found or no changes made");
            }

            await InvalidateCacheAsync(ordinance);

            return Result<Ordinance>.Success(ordinance);
        }
        catch (ValidationException vex)
        {
            return Result<Ordinance>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<Ordinance>.Failure($"Failed to update ordinance: {ex.Message}");
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

            // Get ordinance first to invalidate service area cache
            var ordinance = await _db.QueryFirstOrDefaultAsync<Ordinance>(
                "SELECT * FROM ordinance WHERE id = @Id", new { Id = id });

            var sql = "DELETE FROM ordinance WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });

            if (affected == 0)
            {
                return Result.Failure("Ordinance not found");
            }

            await InvalidateCacheAsync(ordinance, deletedId: id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete ordinance: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(Ordinance? ordinance = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");

        if (ordinance != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{ordinance.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{ordinance.Id}");
            
            if (ordinance.ServiceAreaId.HasValue)
            {
                await _cache.KeyDeleteAsync($"{ServiceAreaCacheKeyPrefix}{ordinance.ServiceAreaId}");
            }
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}