using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace rs_ruralia.ApiService.Services;

public class VendorProfileService
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "vendorprofiles:";
    private const string HistoryCacheKeyPrefix = "vendorprofiles:history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public VendorProfileService(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<VendorProfile>>> GetAllAsync()
    {
        try
        {
            var cacheKey = $"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<VendorProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<VendorProfile>>.Success(cachedData);
            }

            var sql = "SELECT * FROM vendor_profile";
            var result = await _db.QueryAsync<VendorProfile>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<VendorProfile>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<VendorProfile>>.Failure($"Failed to retrieve vendorprofiles: {ex.Message}");
        }
    }

    public async Task<Result<VendorProfile>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<VendorProfile>.Failure("Invalid ID provided");

            var cacheKey = $"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<VendorProfile>(cached.ToString()!);
                return cachedData != null 
                    ? Result<VendorProfile>.Success(cachedData)
                    : Result<VendorProfile>.Failure("VendorProfile not found");
            }

            var sql = "SELECT * FROM vendor_profile WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<VendorProfile>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<VendorProfile>.Success(result);
            }
            
            return Result<VendorProfile>.Failure("VendorProfile not found");
        }
        catch (Exception ex)
        {
            return Result<VendorProfile>.Failure($"Failed to retrieve vendorprofile: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<VendorProfile>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<VendorProfile>>.Failure("Invalid ID provided");

            var cacheKey = $"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<VendorProfile>>(cached.ToString()!)!;
                return Result<IEnumerable<VendorProfile>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM vendor_profile 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<VendorProfile>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<VendorProfile>>.Success(result);
            }
            
            return Result<IEnumerable<VendorProfile>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<VendorProfile>>.Failure($"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<VendorProfile>> CreateAsync(VendorProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<VendorProfile>.Failure(errors);
            }

            var sql = @"
                INSERT INTO vendor_profile (active, name, doing_business_as, vendor_id, license_on_file, license_expiration_date, contractor_license, contractor_license_expiration_date, vendor_type_id, vendor_vin_code_id)
                VALUES (@Active @Name @DoingBusinessAs @VendorId @LicenseOnFile @LicenseExpirationDate @ContractorLicense @ContractorLicenseExpirationDate @VendorTypeId @VendorVinCodeId -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<VendorProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<VendorProfile>.Failure($"Failed to create vendorprofile: {ex.Message}");
        }
    }

    public async Task<Result<VendorProfile>> UpdateAsync(VendorProfile entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<VendorProfile>.Failure(errors);
            }

            var sql = @"
                UPDATE vendor_profile SET
                    active = @Active,                     name = @Name,                     doing_business_as = @DoingBusinessAs,                     vendor_id = @VendorId,                     license_on_file = @LicenseOnFile,                     license_expiration_date = @LicenseExpirationDate,                     contractor_license = @ContractorLicense,                     contractor_license_expiration_date = @ContractorLicenseExpirationDate,                     vendor_type_id = @VendorTypeId,
                    vendor_vin_code_id = @VendorVinCodeId
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<VendorProfile>.Failure("VendorProfile not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<VendorProfile>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<VendorProfile>.Failure($"Failed to update vendorprofile: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM vendor_profile WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("VendorProfile not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete vendorprofile: {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync(VendorProfile? entity = null, int? deletedId = null)
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
