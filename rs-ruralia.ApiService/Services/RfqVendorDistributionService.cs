using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RfqVendorDistributionService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "rfqvendordistributions:";
    private const string HistoryCacheKeyPrefix = "rfqvendordistributions:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RfqVendorDistributionService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<RfqVendorDistribution>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqVendorDistribution>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqVendorDistribution>>.Success(cachedData);
            }

            var sql = "SELECT * FROM rfq_vendor_distribution";
            var result = await _db.QueryAsync<RfqVendorDistribution>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<RfqVendorDistribution>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqVendorDistribution>>.Failure($"Failed to retrieve rfqvendordistributions: {ex.Message}");
        }
    }

    public async Task<Result<RfqVendorDistribution>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<RfqVendorDistribution>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<RfqVendorDistribution>(cached.ToString()!);
                return cachedData != null 
                    ? Result<RfqVendorDistribution>.Success(cachedData)
                    : Result<RfqVendorDistribution>.Failure("RfqVendorDistribution not found");
            }

            var sql = "SELECT * FROM rfq_vendor_distribution WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<RfqVendorDistribution>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<RfqVendorDistribution>.Success(result);
            }
            
            return Result<RfqVendorDistribution>.Failure("RfqVendorDistribution not found");
        }
        catch (Exception ex)
        {
            return Result<RfqVendorDistribution>.Failure($"Failed to retrieve rfqvendordistribution: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<RfqVendorDistribution>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<RfqVendorDistribution>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<RfqVendorDistribution>>(cached.ToString()!)!;
                return Result<IEnumerable<RfqVendorDistribution>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM rfq_vendor_distribution 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<RfqVendorDistribution>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<RfqVendorDistribution>>.Success(result);
            }
            
            return Result<IEnumerable<RfqVendorDistribution>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RfqVendorDistribution>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<RfqVendorDistribution>> CreateAsync(RfqVendorDistribution entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqVendorDistribution>.Failure(errors);
            }

            var sql = @"
                INSERT INTO rfq_vendor_distribution (response_received, declared_non_responsive, awarded, rfq_id, vendor_profile_id, person_profile_vendor_id)
                VALUES (@ResponseReceived @DeclaredNonResponsive @Awarded @RfqId @VendorProfileId @PersonProfileVendorId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<RfqVendorDistribution>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqVendorDistribution>.Failure($"Failed to create rfqvendordistribution: {ex.Message}");
        }
    }

    public async Task<Result<RfqVendorDistribution>> UpdateAsync(RfqVendorDistribution entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<RfqVendorDistribution>.Failure(errors);
            }

            var sql = @"
                UPDATE rfq_vendor_distribution SET
                    response_received = @ResponseReceived,                     declared_non_responsive = @DeclaredNonResponsive,                     awarded = @Awarded,                     rfq_id = @RfqId,                     vendor_profile_id = @VendorProfileId,
                    person_profile_vendor_id = @PersonProfileVendorId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<RfqVendorDistribution>.Failure("RfqVendorDistribution not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<RfqVendorDistribution>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<RfqVendorDistribution>.Failure($"Failed to update rfqvendordistribution: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM rfq_vendor_distribution WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("RfqVendorDistribution not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete rfqvendordistribution: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(RfqVendorDistribution? entity = null, int? deletedId = null)
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
