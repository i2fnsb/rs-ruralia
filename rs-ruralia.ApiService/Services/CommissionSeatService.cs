using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CommissionSeatService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "commissionseat:";
    private const string HistoryCacheKeyPrefix = "commissionseat:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CommissionSeatService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeat>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeat>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commission_seat ORDER BY term_start_date DESC";
            var result = await _db.QueryAsync<CommissionSeat>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionSeat>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Failed to retrieve commission seats: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CommissionSeat>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CommissionSeat>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CommissionSeat>.Success(cachedData)
                    : Result<CommissionSeat>.Failure("Commission seat not found");
            }

            var sql = "SELECT * FROM commission_seat WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CommissionSeat>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CommissionSeat>.Success(result);
            }
            
            return Result<CommissionSeat>.Failure("Commission seat not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Failed to retrieve commission seat: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetByServiceAreaIdAsync(int serviceAreaId)
    {
        try
        {
            if (serviceAreaId <= 0)
                return Result<IEnumerable<CommissionSeat>>.Failure("Invalid Service Area ID provided");

            var cacheKey = $"{CacheKeyPrefix}servicearea:{serviceAreaId}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeat>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeat>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commission_seat WHERE service_area_id = @ServiceAreaId ORDER BY term_start_date DESC";
            var result = await _db.QueryAsync<CommissionSeat>(sql, new { ServiceAreaId = serviceAreaId });
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionSeat>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Failed to retrieve commission seats: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionSeat>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CommissionSeat>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionSeat>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionSeat>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM commission_seat 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CommissionSeat>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CommissionSeat>>.Success(result);
            }
            
            return Result<IEnumerable<CommissionSeat>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionSeat>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> CreateAsync(CommissionSeat commissionSeat)
    {
        try
        {
            if (!ModelValidator.TryValidate(commissionSeat, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeat>.Failure(errors);
            }

            var sql = @"
                INSERT INTO commission_seat 
                (term_start_date, term_end_date, creation_date, termination_date, 
                 commission_seat_type_id, commission_seat_class_id, commission_seat_status_id, service_area_id)
                VALUES 
                (@TermStartDate, @TermEndDate, @CreationDate, @TerminationDate,
                 @CommissionSeatTypeId, @CommissionSeatClassId, @CommissionSeatStatusId, @ServiceAreaId);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            commissionSeat.Id = await _db.ExecuteScalarAsync<int>(sql, commissionSeat);
            await InvalidateCacheAsync();
            
            return Result<CommissionSeat>.Success(commissionSeat);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Failed to create commission seat: {ex.Message}");
        }
    }

    public async Task<Result<CommissionSeat>> UpdateAsync(CommissionSeat commissionSeat)
    {
        try
        {
            if (!ModelValidator.TryValidate(commissionSeat, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionSeat>.Failure(errors);
            }

            var sql = @"
                UPDATE commission_seat SET
                    term_start_date = @TermStartDate,
                    term_end_date = @TermEndDate,
                    creation_date = @CreationDate,
                    termination_date = @TerminationDate,
                    commission_seat_type_id = @CommissionSeatTypeId,
                    commission_seat_class_id = @CommissionSeatClassId,
                    commission_seat_status_id = @CommissionSeatStatusId,
                    service_area_id = @ServiceAreaId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, commissionSeat);
            
            if (affected == 0)
                return Result<CommissionSeat>.Failure("Commission seat not found or no changes made");

            await InvalidateCacheAsync(commissionSeat);
            return Result<CommissionSeat>.Success(commissionSeat);
        }
        catch (Exception ex)
        {
            return Result<CommissionSeat>.Failure($"Failed to update commission seat: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM commission_seat WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("Commission seat not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete commission seat: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CommissionSeat? commissionSeat = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}all");
        
        if (commissionSeat != null)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{commissionSeat.Id}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{commissionSeat.Id}");
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}servicearea:{commissionSeat.ServiceAreaId}");
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync($"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}