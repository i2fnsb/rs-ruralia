using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class RfqService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "rfqs:";
    private const string HistoryCacheKeyPrefix = "rfqs:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public RfqService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<Rfq>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Rfq>>(cached.ToString()!)!;
                return Result<IEnumerable<Rfq>>.Success(cachedData);
            }

            var sql = "SELECT * FROM rfq";
            var result = await _db.QueryAsync<Rfq>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<Rfq>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Rfq>>.Failure($"Failed to retrieve rfqs: {ex.Message}");
        }
    }

    public async Task<Result<Rfq>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<Rfq>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<Rfq>(cached.ToString()!);
                return cachedData != null 
                    ? Result<Rfq>.Success(cachedData)
                    : Result<Rfq>.Failure("Rfq not found");
            }

            var sql = "SELECT * FROM rfq WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Rfq>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<Rfq>.Success(result);
            }
            
            return Result<Rfq>.Failure("Rfq not found");
        }
        catch (Exception ex)
        {
            return Result<Rfq>.Failure($"Failed to retrieve rfq: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Rfq>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<Rfq>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Rfq>>(cached.ToString()!)!;
                return Result<IEnumerable<Rfq>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM rfq 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<Rfq>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<Rfq>>.Success(result);
            }
            
            return Result<IEnumerable<Rfq>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Rfq>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<Rfq>> CreateAsync(Rfq entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Rfq>.Failure(errors);
            }

            var sql = @"
                INSERT INTO rfq (rfq_number, issue_date, opening_date, termination_date, term_fy, renewal_options, custom_project_scope, special_instructions, completed, bid_revised_date, rebid_year, rfq_project_scope_id, rfq_ifb_type_id, service_area_id)
                VALUES (@RfqNumber @IssueDate @OpeningDate @TerminationDate @TermFy @RenewalOptions @CustomProjectScope @SpecialInstructions @Completed @BidRevisedDate @RebidYear @RfqProjectScopeId @RfqIfbTypeId @ServiceAreaId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<Rfq>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Rfq>.Failure($"Failed to create rfq: {ex.Message}");
        }
    }

    public async Task<Result<Rfq>> UpdateAsync(Rfq entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<Rfq>.Failure(errors);
            }

            var sql = @"
                UPDATE rfq SET
                    rfq_number = @RfqNumber,                     issue_date = @IssueDate,                     opening_date = @OpeningDate,                     termination_date = @TerminationDate,                     term_fy = @TermFy,                     renewal_options = @RenewalOptions,                     custom_project_scope = @CustomProjectScope,                     special_instructions = @SpecialInstructions,                     completed = @Completed,                     bid_revised_date = @BidRevisedDate,                     rebid_year = @RebidYear,                     rfq_project_scope_id = @RfqProjectScopeId,                     rfq_ifb_type_id = @RfqIfbTypeId,
                    service_area_id = @ServiceAreaId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<Rfq>.Failure("Rfq not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<Rfq>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<Rfq>.Failure($"Failed to update rfq: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM rfq WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("Rfq not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete rfq: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(Rfq? entity = null, int? deletedId = null)
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
