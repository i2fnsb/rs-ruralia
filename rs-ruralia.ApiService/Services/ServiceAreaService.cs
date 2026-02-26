using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class ServiceAreaService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "servicearea:";
    private const string HistoryCacheKeyPrefix = "servicearea:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1); // History changes less frequently

    public ServiceAreaService(
        IDbConnection db, 
        IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<ServiceArea>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<ServiceArea>>(cached.ToString()!)!;
                return Result<IEnumerable<ServiceArea>>.Success(cachedData);
            }

            var sql = "SELECT * FROM service_area ORDER BY name";
            var result = await _db.QueryAsync<ServiceArea>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<ServiceArea>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceArea>>.Failure($"Failed to retrieve service areas: {ex.Message}");
        }
    }

    public async Task<Result<ServiceArea>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<ServiceArea>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<ServiceArea>(cached.ToString()!);
                return cachedData != null 
                    ? Result<ServiceArea>.Success(cachedData)
                    : Result<ServiceArea>.Failure("Service area not found");
            }

            var sql = "SELECT * FROM service_area WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<ServiceArea>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<ServiceArea>.Success(result);
            }
            
            return Result<ServiceArea>.Failure("Service area not found");
        }
        catch (Exception ex)
        {
            return Result<ServiceArea>.Failure($"Failed to retrieve service area: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ServiceArea>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<IEnumerable<ServiceArea>>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<ServiceArea>>(cached.ToString()!)!;
                return Result<IEnumerable<ServiceArea>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM service_area 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<ServiceArea>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<ServiceArea>>.Success(result);
            }
            
            return Result<IEnumerable<ServiceArea>>.Failure("No history found for service area");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceArea>>.Failure($"Failed to retrieve service area history: {ex.Message}");
        }
    }

    public async Task<Result<ServiceArea>> GetHistoryByIdAtPointInTimeAsync(int id, DateTime asOfDate)
    {
        try
        {
            if (id <= 0)
            {
                return Result<ServiceArea>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}:at:{asOfDate:yyyyMMddHHmmss}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<ServiceArea>(cached.ToString()!);
                return cachedData != null 
                    ? Result<ServiceArea>.Success(cachedData)
                    : Result<ServiceArea>.Failure("Service area not found at specified time");
            }

            var sql = @"
                SELECT * FROM service_area 
                FOR SYSTEM_TIME AS OF @AsOfDate
                WHERE id = @Id";
            
            var result = await _db.QueryFirstOrDefaultAsync<ServiceArea>(sql, new { Id = id, AsOfDate = asOfDate });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<ServiceArea>.Success(result);
            }
            
            return Result<ServiceArea>.Failure("Service area not found at specified time");
        }
        catch (Exception ex)
        {
            return Result<ServiceArea>.Failure($"Failed to retrieve service area at point in time: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ServiceArea>>> GetHistoryBetweenDatesAsync(int id, DateTime startDate, DateTime endDate)
    {
        try
        {
            if (id <= 0)
            {
                return Result<IEnumerable<ServiceArea>>.Failure("Invalid ID provided");
            }

            if (startDate >= endDate)
            {
                return Result<IEnumerable<ServiceArea>>.Failure("Start date must be before end date");
            }

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}:between:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<ServiceArea>>(cached.ToString()!)!;
                return Result<IEnumerable<ServiceArea>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM service_area 
                FOR SYSTEM_TIME BETWEEN @StartDate AND @EndDate
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<ServiceArea>(sql, new { Id = id, StartDate = startDate, EndDate = endDate });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<ServiceArea>>.Success(result);
            }
            
            return Result<IEnumerable<ServiceArea>>.Failure("No history found for service area in specified date range");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceArea>>.Failure($"Failed to retrieve service area history: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<ServiceArea>>> GetAllHistoryAsync()
    {
        try
        {
            var cacheKey = $"{HistoryCacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<ServiceArea>>(cached.ToString()!)!;
                return Result<IEnumerable<ServiceArea>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM service_area 
                FOR SYSTEM_TIME ALL
                ORDER BY id, ValidFrom DESC";
            
            var result = await _db.QueryAsync<ServiceArea>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
            
            return Result<IEnumerable<ServiceArea>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceArea>>.Failure($"Failed to retrieve all service area history: {ex.Message}");
        }
    }

    public async Task<Result<ServiceArea>> CreateAsync(ServiceArea serviceArea)
    {
        try
        {
            if (!ModelValidator.TryValidate(serviceArea, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<ServiceArea>.Failure(errors);
            }

            var sql = @"
                INSERT INTO service_area 
                (feature_object_id, name, org_key, current_mileage, maximum_mill_rate, 
                 current_mill_rate, fire_mill_rate, mill_levy, tax_authority_code, 
                 initial_tax_year, match_feature_layer, service_area_code_id)
                VALUES 
                (@FeatureObjectId, @Name, @OrgKey, @CurrentMileage, @MaximumMillRate,
                 @CurrentMillRate, @FireMillRate, @MillLevy, @TaxAuthorityCode,
                 @InitialTaxYear, @MatchFeatureLayer, @ServiceAreaCodeId);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            serviceArea.Id = await _db.ExecuteScalarAsync<int>(sql, serviceArea);
            await InvalidateCacheAsync();
            
            return Result<ServiceArea>.Success(serviceArea);
        }
        catch (ValidationException vex)
        {
            return Result<ServiceArea>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<ServiceArea>.Failure($"Failed to create service area: {ex.Message}");
        }
    }

    public async Task<Result<ServiceArea>> UpdateAsync(ServiceArea serviceArea)
    {
        try
        {
            if (!ModelValidator.TryValidate(serviceArea, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<ServiceArea>.Failure(errors);
            }

            var sql = @"
                UPDATE service_area SET
                    feature_object_id = @FeatureObjectId,
                    name = @Name,
                    org_key = @OrgKey,
                    current_mileage = @CurrentMileage,
                    maximum_mill_rate = @MaximumMillRate,
                    current_mill_rate = @CurrentMillRate,
                    fire_mill_rate = @FireMillRate,
                    mill_levy = @MillLevy,
                    tax_authority_code = @TaxAuthorityCode,
                    initial_tax_year = @InitialTaxYear,
                    match_feature_layer = @MatchFeatureLayer,
                    service_area_code_id = @ServiceAreaCodeId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, serviceArea);
            
            if (affected == 0)
            {
                return Result<ServiceArea>.Failure("Service area not found or no changes made");
            }

            await InvalidateCacheAsync(serviceArea);
            
            return Result<ServiceArea>.Success(serviceArea);
        }
        catch (ValidationException vex)
        {
            return Result<ServiceArea>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<ServiceArea>.Failure($"Failed to update service area: {ex.Message}");
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

            var sql = "DELETE FROM service_area WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
            {
                return Result.Failure("Service area not found");
            }

            await InvalidateCacheAsync(deletedId: id);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete service area: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(ServiceArea? serviceArea = null, int? deletedId = null)
    {
        // Invalidate current data cache
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        
        // Invalidate history caches when data changes
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");
        
        if (serviceArea != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{serviceArea.Id}");
            await InvalidateHistoryCacheByIdAsync(serviceArea.Id);
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await InvalidateHistoryCacheByIdAsync(deletedId.Value);
        }
    }

    private async Task InvalidateHistoryCacheByIdAsync(int id)
    {
        // Invalidate specific history cache entries for the ID
        // Note: We can't invalidate time-based queries without scanning, 
        // but the main history cache should be invalidated
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{id}");
        
        // Optional: Use key patterns to delete all related history keys
        // This requires the server to support key scanning (SCAN command)
        // For production, consider using Redis Tags or separate tracking
    }
}