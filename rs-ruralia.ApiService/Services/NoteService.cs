using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class NoteService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "note:";
    private const string EntityCacheKeyPrefix = "note:entity:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public NoteService(
        IDbConnection db,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<Note>>> GetByEntityAsync(string entityType, int entityId)
    {
        try
        {
            var cacheKey = $"{EntityCacheKeyPrefix}{entityType}:{entityId}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<Note>>(cached.ToString()!)!;
                return Result<IEnumerable<Note>>.Success(cachedData);
            }

            var columnName = GetColumnNameForEntityType(entityType);
            if (columnName == null)
            {
                return Result<IEnumerable<Note>>.Failure($"Invalid entity type: {entityType}");
            }

            var sql = $"SELECT * FROM note WHERE {columnName} = @EntityId ORDER BY ValidFrom DESC";
            var result = await _db.QueryAsync<Note>(sql, new { EntityId = entityId });
            
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<Note>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Note>>.Failure($"Failed to retrieve notes: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Note>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            var sql = @"
                SELECT * FROM note FOR SYSTEM_TIME ALL 
                WHERE id = @Id 
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<Note>(sql, new { Id = id });
            return Result<IEnumerable<Note>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<Note>>.Failure($"Failed to retrieve note history: {ex.Message}");
        }
    }

    public async Task<Result<Note>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
            {
                return Result<Note>.Failure("Invalid ID provided");
            }

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);

            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<Note>(cached.ToString()!);
                return cachedData != null
                    ? Result<Note>.Success(cachedData)
                    : Result<Note>.Failure("Note not found");
            }

            var sql = "SELECT * FROM note WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<Note>(sql, new { Id = id });

            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<Note>.Success(result);
            }

            return Result<Note>.Failure("Note not found");
        }
        catch (Exception ex)
        {
            return Result<Note>.Failure($"Failed to retrieve note: {ex.Message}");
        }
    }

    public async Task<Result<Note>> CreateAsync(Note note)
    {
        try
        {
            var sql = @"
                INSERT INTO note (note, road_id, service_area_id, commissioner_profile_id, 
                    commission_seat_id, correspondence_profile_id, person_profile_id, 
                    vendor_profile_id, rfq_id, ordinance_id, ModifiedBy)
                OUTPUT INSERTED.*
                VALUES (@NoteText, @RoadId, @ServiceAreaId, @CommissionerProfileId, 
                    @CommissionSeatId, @CorrespondenceProfileId, @PersonProfileId, 
                    @VendorProfileId, @RfqId, @OrdinanceId, @ModifiedBy)";

            var result = await _db.QueryFirstOrDefaultAsync<Note>(sql, note);

            if (result != null)
            {
                await InvalidateEntityCache(note);
                return Result<Note>.Success(result);
            }

            return Result<Note>.Failure("Failed to create note");
        }
        catch (Exception ex)
        {
            return Result<Note>.Failure($"Failed to create note: {ex.Message}");
        }
    }

    public async Task<Result<Note>> UpdateAsync(Note note)
    {
        try
        {
            var sql = @"
                UPDATE note 
                SET note = @NoteText,
                    road_id = @RoadId,
                    service_area_id = @ServiceAreaId,
                    commissioner_profile_id = @CommissionerProfileId,
                    commission_seat_id = @CommissionSeatId,
                    correspondence_profile_id = @CorrespondenceProfileId,
                    person_profile_id = @PersonProfileId,
                    vendor_profile_id = @VendorProfileId,
                    rfq_id = @RfqId,
                    ordinance_id = @OrdinanceId,
                    ModifiedBy = @ModifiedBy
                WHERE id = @Id";

            var affectedRows = await _db.ExecuteAsync(sql, note);

            if (affectedRows > 0)
            {
                await InvalidateCache(note.Id);
                await InvalidateEntityCache(note);
                return Result<Note>.Success(note);
            }

            return Result<Note>.Failure("Note not found");
        }
        catch (Exception ex)
        {
            return Result<Note>.Failure($"Failed to update note: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            // Get note before deletion for cache invalidation
            var note = await _db.QueryFirstOrDefaultAsync<Note>(
                "SELECT * FROM note WHERE id = @Id", 
                new { Id = id });

            var sql = "DELETE FROM notes WHERE id = @Id";
            var affectedRows = await _db.ExecuteAsync(sql, new { Id = id });

            if (affectedRows > 0)
            {
                await InvalidateCache(id);
                if (note != null)
                {
                    await InvalidateEntityCache(note);
                }
                return Result.Success();
            }

            return Result.Failure("Note not found");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete note: {ex.Message}");
        }
    }

    private async Task InvalidateCache(int id)
    {
        await _cache.KeyDeleteAsync($"{CacheKeyPrefix}{id}");
    }

    private async Task InvalidateEntityCache(Note note)
    {
        var entityTypes = new[]
        {
            ("road", note.RoadId),
            ("service_area", note.ServiceAreaId),
            ("commissioner_profile", note.CommissionerProfileId),
            ("commission_seat", note.CommissionSeatId),
            ("correspondence_profile", note.CorrespondenceProfileId),
            ("person_profile", note.PersonProfileId),
            ("vendor_profile", note.VendorProfileId),
            ("rfq", note.RfqId),
            ("ordinance", note.OrdinanceId)
        };

        foreach (var (entityType, entityId) in entityTypes)
        {
            if (entityId.HasValue)
            {
                await _cache.KeyDeleteAsync($"{EntityCacheKeyPrefix}{entityType}:{entityId.Value}");
            }
        }
    }

    private static string? GetColumnNameForEntityType(string entityType) => entityType.ToLowerInvariant() switch
    {
        "road" => "road_id",
        "service_area" or "servicearea" => "service_area_id",
        "commissioner_profile" or "commissionerprofile" => "commissioner_profile_id",
        "commission_seat" or "commissionseat" => "commission_seat_id",
        "correspondence_profile" or "correspondenceprofile" => "correspondence_profile_id",
        "person_profile" or "personprofile" => "person_profile_id",
        "vendor_profile" or "vendorprofile" => "vendor_profile_id",
        "rfq" => "rfq_id",
        "ordinance" => "ordinance_id",
        _ => null
    };
}
