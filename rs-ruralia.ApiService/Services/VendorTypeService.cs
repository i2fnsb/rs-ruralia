using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class VendorTypeService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "vendortypes:";
    private const string HistoryCacheKeyPrefix = "vendortypes:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public VendorTypeService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<VendorType>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<VendorType>>(cached.ToString()!)!;
                return Result<IEnumerable<VendorType>>.Success(cachedData);
            }

            var sql = "SELECT * FROM vendor_type";
            var result = await _db.QueryAsync<VendorType>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<VendorType>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<VendorType>>.Failure($"Failed to retrieve vendortypes: {ex.Message}");
        }
    }

    public async Task<Result<VendorType>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<VendorType>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<VendorType>(cached.ToString()!);
                return cachedData != null 
                    ? Result<VendorType>.Success(cachedData)
                    : Result<VendorType>.Failure("VendorType not found");
            }

            var sql = "SELECT * FROM vendor_type WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<VendorType>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<VendorType>.Success(result);
            }
            
            return Result<VendorType>.Failure("VendorType not found");
        }
        catch (Exception ex)
        {
            return Result<VendorType>.Failure($"Failed to retrieve vendortype: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<VendorType>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<VendorType>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<VendorType>>(cached.ToString()!)!;
                return Result<IEnumerable<VendorType>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM vendor_type 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<VendorType>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<VendorType>>.Success(result);
            }
            
            return Result<IEnumerable<VendorType>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<VendorType>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<VendorType>> CreateAsync(VendorType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<VendorType>.Failure(errors);
            }

            var sql = @"
                INSERT INTO vendor_type (type)
                VALUES (@Type -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<VendorType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<VendorType>.Failure($"Failed to create vendortype: {ex.Message}");
        }
    }

    public async Task<Result<VendorType>> UpdateAsync(VendorType entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<VendorType>.Failure(errors);
            }

            var sql = @"
                UPDATE vendor_type SET

                    type = @Type
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<VendorType>.Failure("VendorType not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<VendorType>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<VendorType>.Failure($"Failed to update vendortype: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM vendor_type WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("VendorType not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete vendortype: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(VendorType? entity = null, int? deletedId = null)
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
