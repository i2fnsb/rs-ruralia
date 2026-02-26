using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class ServiceAreaCodeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "serviceareacode:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public ServiceAreaCodeService(
        IDbConnection db,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<ServiceAreaCode>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<ServiceAreaCode>>(cached.ToString()!)!;
                return Result<IEnumerable<ServiceAreaCode>>.Success(cachedData);
            }

            var sql = "SELECT * FROM service_area_code ORDER BY code";
            var result = await _db.QueryAsync<ServiceAreaCode>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);

            return Result<IEnumerable<ServiceAreaCode>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ServiceAreaCode>>.Failure($"Failed to retrieve service area codes: {ex.Message}");
        }
    }

    public async Task<Result<ServiceAreaCode>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<ServiceAreaCode>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<ServiceAreaCode>(cached.ToString()!);
                return cachedData != null
                    ? Result<ServiceAreaCode>.Success(cachedData)
                    : Result<ServiceAreaCode>.Failure("Service area code not found");
            }

            var sql = "SELECT * FROM service_area_code WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<ServiceAreaCode>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<ServiceAreaCode>.Success(result);
            }

            return Result<ServiceAreaCode>.Failure("Service area code not found");
        }
        catch (Exception ex)
        {
            return Result<ServiceAreaCode>.Failure($"Failed to retrieve service area code: {ex.Message}");
        }
    }

    public async Task<Result<ServiceAreaCode>> GetByCodeAsync(string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Result<ServiceAreaCode>.Failure("Invalid code provided");
            }

            var cacheKey = $"{CacheKeyPrefix}code:{code}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<ServiceAreaCode>(cached.ToString()!);
                return cachedData != null
                    ? Result<ServiceAreaCode>.Success(cachedData)
                    : Result<ServiceAreaCode>.Failure("Service area code not found");
            }

            var sql = "SELECT * FROM service_area_code WHERE code = @Code";
            var result = await _db.QueryFirstOrDefaultAsync<ServiceAreaCode>(sql, new { Code = code });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<ServiceAreaCode>.Success(result);
            }

            return Result<ServiceAreaCode>.Failure("Service area code not found");
        }
        catch (Exception ex)
        {
            return Result<ServiceAreaCode>.Failure($"Failed to retrieve service area code: {ex.Message}");
        }
    }

    public async Task<Result<ServiceAreaCode>> CreateAsync(ServiceAreaCode serviceAreaCode)
    {
        try
        {
            if (!ModelValidator.TryValidate(serviceAreaCode, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<ServiceAreaCode>.Failure(errors);
            }

            var sql = @"
                INSERT INTO service_area_code 
                (code, description, ModifiedBy)
                VALUES 
                (@Code, @Description, @ModifiedBy);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            serviceAreaCode.Id = await _db.ExecuteScalarAsync<int>(sql, serviceAreaCode);
            await InvalidateCacheAsync();

            return Result<ServiceAreaCode>.Success(serviceAreaCode);
        }
        catch (ValidationException vex)
        {
            return Result<ServiceAreaCode>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<ServiceAreaCode>.Failure($"Failed to create service area code: {ex.Message}");
        }
    }

    public async Task<Result<ServiceAreaCode>> UpdateAsync(ServiceAreaCode serviceAreaCode)
    {
        try
        {
            if (!ModelValidator.TryValidate(serviceAreaCode, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<ServiceAreaCode>.Failure(errors);
            }

            var sql = @"
                UPDATE service_area_code SET
                    code = @Code,
                    description = @Description,
                    ModifiedBy = @ModifiedBy
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, serviceAreaCode);

            if (affected == 0)
            {
                return Result<ServiceAreaCode>.Failure("Service area code not found or no changes made");
            }

            await InvalidateCacheAsync(serviceAreaCode);

            return Result<ServiceAreaCode>.Success(serviceAreaCode);
        }
        catch (ValidationException vex)
        {
            return Result<ServiceAreaCode>.Failure($"Validation failed: {vex.Message}");
        }
        catch (Exception ex)
        {
            return Result<ServiceAreaCode>.Failure($"Failed to update service area code: {ex.Message}");
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

            // Check if code is being used by any service areas
            var usageCheckSql = "SELECT COUNT(*) FROM service_area WHERE service_area_code_id = @Id";
            var usageCount = await _db.ExecuteScalarAsync<int>(usageCheckSql, new { Id = id });

            if (usageCount > 0)
            {
                return Result.Failure($"Cannot delete service area code. It is being used by {usageCount} service area(s).");
            }

            var sql = "DELETE FROM service_area_code WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });

            if (affected == 0)
            {
                return Result.Failure("Service area code not found");
            }

            await InvalidateCacheAsync(deletedId: id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete service area code: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(ServiceAreaCode? serviceAreaCode = null, int? deletedId = null)
    {
        // Invalidate all list caches
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}active");

        if (serviceAreaCode != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{serviceAreaCode.Id}");
            if (!string.IsNullOrWhiteSpace(serviceAreaCode.Code))
            {
                await _cache.KeyDeleteAsync($"{CacheKeyPrefix}code:{serviceAreaCode.Code}");
            }
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
        }
    }
}