using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class BidService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "bids:";
    private const string HistoryCacheKeyPrefix = "bids:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public BidService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<Bid>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Bid>>(cached.ToString()!)!;
                return Result<IEnumerable<Bid>>.Success(cachedData);
            }

            var sql = "SELECT * FROM bid";
            var result = await _db.QueryAsync<Bid>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<Bid>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Bid>>.Failure($"Failed to retrieve bids: {ex.Message}");
        }
    }

    public async Task<Result<Bid>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<Bid>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<Bid>(cached.ToString()!);
                return cachedData != null 
                    ? Result<Bid>.Success(cachedData)
                    : Result<Bid>.Failure("Bid not found");
            }

            var sql = "SELECT * FROM bid WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Bid>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<Bid>.Success(result);
            }
            
            return Result<Bid>.Failure("Bid not found");
        }
        catch (Exception ex)
        {
            return Result<Bid>.Failure($"Failed to retrieve bid: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Bid>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<Bid>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Bid>>(cached.ToString()!)!;
                return Result<IEnumerable<Bid>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM bid 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<Bid>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<Bid>>.Success(result);
            }
            
            return Result<IEnumerable<Bid>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Bid>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<Bid>> CreateAsync(Bid entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Bid>.Failure(errors);
            }

            var sql = @"
                INSERT INTO bid (bid_amount, rfq_id, vendor_profile_id, specification_pay_item_id, bid_quantity_id)
                VALUES (@BidAmount @RfqId @VendorProfileId @SpecificationPayItemId @BidQuantityId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<Bid>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Bid>.Failure($"Failed to create bid: {ex.Message}");
        }
    }

    public async Task<Result<Bid>> UpdateAsync(Bid entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Bid>.Failure(errors);
            }

            var sql = @"
                UPDATE bid SET
                    bid_amount = @BidAmount,                     rfq_id = @RfqId,                     vendor_profile_id = @VendorProfileId,                     specification_pay_item_id = @SpecificationPayItemId,
                    bid_quantity_id = @BidQuantityId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<Bid>.Failure("Bid not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<Bid>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Bid>.Failure($"Failed to update bid: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM bid WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("Bid not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete bid: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(Bid? entity = null, int? deletedId = null)
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
