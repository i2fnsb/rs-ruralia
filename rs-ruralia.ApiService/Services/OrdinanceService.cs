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
    private readonly ILogger<OrdinanceService> _logger;
    private const string CacheKeyPrefix = "ordinance:";
    private const string HistoryCacheKeyPrefix = "ordinance:history:";
    private const string ServiceAreaCacheKeyPrefix = "ordinance:servicearea:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public OrdinanceService(
        IDbConnection db,
        IConnectionMultiplexer redis,
        ILogger<OrdinanceService> logger)
    {
        _db = db;
        _cache = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetAllAsync()
    {
        _logger.LogInformation("GetAllAsync: Retrieving all ordinances");
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                _logger.LogInformation("GetAllAsync: Cache hit for all ordinances");
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            _logger.LogInformation("GetAllAsync: Cache miss, querying database");
            var sql = "SELECT * FROM ordinance ORDER BY ordinance_year DESC, ordinance";
            var result = await _db.QueryAsync<Ordinance>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            _logger.LogInformation("GetAllAsync: Retrieved {Count} ordinances from database", result.Count());

            return Result<IEnumerable<Ordinance>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllAsync: Failed to retrieve ordinances");
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinances: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> GetByIdAsync(int id)
    {
        _logger.LogInformation("GetByIdAsync: Retrieving ordinance with ID {Id}", id);
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("GetByIdAsync: Invalid ID {Id} provided", id);
                return Result<Ordinance>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                _logger.LogInformation("GetByIdAsync: Cache hit for ordinance ID {Id}", id);
                var cachedData = JsonSerializer.Deserialize<Ordinance>(cached.ToString()!);
                return cachedData != null
                    ? Result<Ordinance>.Success(cachedData)
                    : Result<Ordinance>.Failure("Ordinance not found");
            }

            _logger.LogInformation("GetByIdAsync: Cache miss for ID {Id}, querying database", id);
            var sql = "SELECT * FROM ordinance WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Ordinance>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                _logger.LogInformation("GetByIdAsync: Successfully retrieved ordinance ID {Id}", id);
                return Result<Ordinance>.Success(result);
            }

            _logger.LogWarning("GetByIdAsync: Ordinance ID {Id} not found", id);
            return Result<Ordinance>.Failure("Ordinance not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByIdAsync: Failed to retrieve ordinance ID {Id}", id);
            return Result<Ordinance>.Failure($"Failed to retrieve ordinance: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetByServiceAreaIdAsync(int serviceAreaId)
    {
        _logger.LogInformation("GetByServiceAreaIdAsync: Retrieving ordinances for service area ID {ServiceAreaId}", serviceAreaId);
        try
        {
            if (serviceAreaId <= 0)
            {
                _logger.LogWarning("GetByServiceAreaIdAsync: Invalid service area ID {ServiceAreaId} provided", serviceAreaId);
                return Result<IEnumerable<Ordinance>>.Failure("Invalid Service Area ID provided");
            }

            var cacheKey = $"{ServiceAreaCacheKeyPrefix}{serviceAreaId}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                _logger.LogInformation("GetByServiceAreaIdAsync: Cache hit for service area ID {ServiceAreaId}", serviceAreaId);
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            _logger.LogInformation("GetByServiceAreaIdAsync: Cache miss for service area ID {ServiceAreaId}, querying database", serviceAreaId);
            var sql = "SELECT * FROM ordinance WHERE service_area_id = @ServiceAreaId ORDER BY ordinance_year DESC, ordinance";
            var result = await _db.QueryAsync<Ordinance>(sql, new { ServiceAreaId = serviceAreaId });
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            _logger.LogInformation("GetByServiceAreaIdAsync: Retrieved {Count} ordinances for service area ID {ServiceAreaId}", result.Count(), serviceAreaId);

            return Result<IEnumerable<Ordinance>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByServiceAreaIdAsync: Failed to retrieve ordinances for service area ID {ServiceAreaId}", serviceAreaId);
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinances for service area: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Ordinance>>> GetHistoryByIdAsync(int id)
    {
        _logger.LogInformation("GetHistoryByIdAsync: Retrieving history for ordinance ID {Id}", id);
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("GetHistoryByIdAsync: Invalid ID {Id} provided", id);
                return Result<IEnumerable<Ordinance>>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                _logger.LogInformation("GetHistoryByIdAsync: Cache hit for ordinance history ID {Id}", id);
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Ordinance>>(cached.ToString()!)!;
                return Result<IEnumerable<Ordinance>>.Success(cachedData);
            }

            _logger.LogInformation("GetHistoryByIdAsync: Cache miss for ID {Id}, querying database", id);
            var sql = @"
                SELECT * FROM ordinance 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";

            var result = await _db.QueryAsync<Ordinance>(sql, new { Id = id });

            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                _logger.LogInformation("GetHistoryByIdAsync: Retrieved {Count} history records for ordinance ID {Id}", result.Count(), id);
                return Result<IEnumerable<Ordinance>>.Success(result);
            }

            _logger.LogWarning("GetHistoryByIdAsync: No history found for ordinance ID {Id}", id);
            return Result<IEnumerable<Ordinance>>.Failure("No history found for ordinance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHistoryByIdAsync: Failed to retrieve history for ordinance ID {Id}", id);
            return Result<IEnumerable<Ordinance>>.Failure($"Failed to retrieve ordinance history: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> CreateAsync(Ordinance ordinance)
    {
        _logger.LogInformation("CreateAsync: Creating new ordinance {OrdinanceName} for year {Year}", ordinance.OrdinanceName, ordinance.OrdinanceYear);
        try
        {
            if (!ModelValidator.TryValidate(ordinance, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                _logger.LogWarning("CreateAsync: Validation failed for ordinance {OrdinanceName}: {Errors}", ordinance.OrdinanceName, string.Join(", ", errors));
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
            _logger.LogInformation("CreateAsync: Successfully created ordinance with ID {Id}", ordinance.Id);

            return Result<Ordinance>.Success(ordinance);
        }
        catch (ValidationException vex)
        {
            _logger.LogError(vex, "CreateAsync: Validation exception for ordinance {OrdinanceName}", ordinance.OrdinanceName);
            return Result<Ordinance>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync: Failed to create ordinance {OrdinanceName}", ordinance.OrdinanceName);
            return Result<Ordinance>.Failure($"Failed to create ordinance: {ex.Message}");
        }
    }

    public async Task<Result<Ordinance>> UpdateAsync(Ordinance ordinance)
    {
        _logger.LogInformation("UpdateAsync: Updating ordinance ID {Id}", ordinance.Id);
        try
        {
            if (!ModelValidator.TryValidate(ordinance, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                _logger.LogWarning("UpdateAsync: Validation failed for ordinance ID {Id}: {Errors}", ordinance.Id, string.Join(", ", errors));
                return Result<Ordinance>.Failure(errors);
            }

            // Get the OLD ordinance before updating (for cache invalidation of old service area)
            var oldOrdinance = await _db.QueryFirstOrDefaultAsync<Ordinance>(
                "SELECT * FROM ordinance WHERE id = @Id", new { Id = ordinance.Id });

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
                _logger.LogWarning("UpdateAsync: Ordinance ID {Id} not found or no changes made", ordinance.Id);
                return Result<Ordinance>.Failure("Ordinance not found or no changes made");
            }

            // Invalidate cache for both OLD and NEW service areas
            await InvalidateCacheAsync(ordinance, oldServiceAreaId: oldOrdinance?.ServiceAreaId);
            _logger.LogInformation("UpdateAsync: Successfully updated ordinance ID {Id}", ordinance.Id);

            return Result<Ordinance>.Success(ordinance);
        }
        catch (ValidationException vex)
        {
            _logger.LogError(vex, "UpdateAsync: Validation exception for ordinance ID {Id}", ordinance.Id);
            return Result<Ordinance>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync: Failed to update ordinance ID {Id}", ordinance.Id);
            return Result<Ordinance>.Failure($"Failed to update ordinance: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync: Deleting ordinance ID {Id}", id);
        try
        {
            if (id <= 0)
            {
                _logger.LogWarning("DeleteAsync: Invalid ID {Id} provided", id);
                return Result.Failure("Invalid ID provided");
            }

            // Get ordinance first to invalidate service area cache
            var ordinance = await _db.QueryFirstOrDefaultAsync<Ordinance>(
                "SELECT * FROM ordinance WHERE id = @Id", new { Id = id });

            var sql = "DELETE FROM ordinance WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });

            if (affected == 0)
            {
                _logger.LogWarning("DeleteAsync: Ordinance ID {Id} not found", id);
                return Result.Failure("Ordinance not found");
            }

            await InvalidateCacheAsync(ordinance, deletedId: id);
            _logger.LogInformation("DeleteAsync: Successfully deleted ordinance ID {Id}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync: Failed to delete ordinance ID {Id}", id);
            return Result.Failure($"Failed to delete ordinance: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(Ordinance? ordinance = null, int? deletedId = null, int? oldServiceAreaId = null)
    {
        _logger.LogDebug("InvalidateCacheAsync: Invalidating cache for ordinance ID {Id}", ordinance?.Id ?? deletedId);
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");

        if (ordinance != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{ordinance.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{ordinance.Id}");

            // Invalidate NEW service area cache
            if (ordinance.ServiceAreaId.HasValue)
            {
                await _cache.KeyDeleteAsync($"{ServiceAreaCacheKeyPrefix}{ordinance.ServiceAreaId}");
                _logger.LogDebug("InvalidateCacheAsync: Invalidated cache for NEW service area {ServiceAreaId}", ordinance.ServiceAreaId);
            }

            // Invalidate OLD service area cache (when transferring between service areas)
            if (oldServiceAreaId.HasValue && oldServiceAreaId != ordinance.ServiceAreaId)
            {
                await _cache.KeyDeleteAsync($"{ServiceAreaCacheKeyPrefix}{oldServiceAreaId}");
                _logger.LogDebug("InvalidateCacheAsync: Invalidated cache for OLD service area {OldServiceAreaId}", oldServiceAreaId);
            }
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
        _logger.LogDebug("InvalidateCacheAsync: Cache invalidation complete");
    }
}