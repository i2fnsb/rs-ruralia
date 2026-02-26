using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class CommissionerProfileService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "commissionerprofiles:";
    private const string HistoryCacheKeyPrefix = "commissionerprofiles:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public CommissionerProfileService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<CommissionerProfile>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionerProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionerProfile>>.Success(cachedData);
            }

            var sql = "SELECT * FROM commissioner_profile";
            var result = await _db.QueryAsync<CommissionerProfile>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<CommissionerProfile>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionerProfile>>.Failure($"Failed to retrieve commissionerprofiles: {ex.Message}");
        }
    }

    public async Task<Result<CommissionerProfile>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<CommissionerProfile>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<CommissionerProfile>(cached.ToString()!);
                return cachedData != null 
                    ? Result<CommissionerProfile>.Success(cachedData)
                    : Result<CommissionerProfile>.Failure("CommissionerProfile not found");
            }

            var sql = "SELECT * FROM commissioner_profile WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<CommissionerProfile>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<CommissionerProfile>.Success(result);
            }
            
            return Result<CommissionerProfile>.Failure("CommissionerProfile not found");
        }
        catch (Exception ex)
        {
            return Result<CommissionerProfile>.Failure($"Failed to retrieve commissionerprofile: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<CommissionerProfile>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<CommissionerProfile>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<CommissionerProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<CommissionerProfile>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM commissioner_profile 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<CommissionerProfile>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<CommissionerProfile>>.Success(result);
            }
            
            return Result<IEnumerable<CommissionerProfile>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CommissionerProfile>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<CommissionerProfile>> CreateAsync(CommissionerProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionerProfile>.Failure(errors);
            }

            var sql = @"
                INSERT INTO commissioner_profile (oath, invoices, resides_in_service_area, registered_voter, recommended_for_appointment, do_not_appoint, do_not_appoint_details, appointed_date, assembly_meeting_date, effective_date, end_date, commissioner_status_id, commission_seat_id, person_profile_id)
                VALUES (@Oath @Invoices @ResidesInServiceArea @RegisteredVoter @RecommendedForAppointment @DoNotAppoint @DoNotAppointDetails @AppointedDate @AssemblyMeetingDate @EffectiveDate @EndDate @CommissionerStatusId @CommissionSeatId @PersonProfileId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<CommissionerProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionerProfile>.Failure($"Failed to create commissionerprofile: {ex.Message}");
        }
    }

    public async Task<Result<CommissionerProfile>> UpdateAsync(CommissionerProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<CommissionerProfile>.Failure(errors);
            }

            var sql = @"
                UPDATE commissioner_profile SET
                    oath = @Oath,                     invoices = @Invoices,                     resides_in_service_area = @ResidesInServiceArea,                     registered_voter = @RegisteredVoter,                     recommended_for_appointment = @RecommendedForAppointment,                     do_not_appoint = @DoNotAppoint,                     do_not_appoint_details = @DoNotAppointDetails,                     appointed_date = @AppointedDate,                     assembly_meeting_date = @AssemblyMeetingDate,                     effective_date = @EffectiveDate,                     end_date = @EndDate,                     commissioner_status_id = @CommissionerStatusId,                     commission_seat_id = @CommissionSeatId,
                    person_profile_id = @PersonProfileId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<CommissionerProfile>.Failure("CommissionerProfile not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<CommissionerProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<CommissionerProfile>.Failure($"Failed to update commissionerprofile: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM commissioner_profile WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("CommissionerProfile not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete commissionerprofile: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(CommissionerProfile? entity = null, int? deletedId = null)
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
