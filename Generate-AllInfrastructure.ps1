# PowerShell Script to Generate All Infrastructure Files
# Run this from the solution root directory

$models = @(
    @{ Name="CommissionSeatClass"; Table="commission_seat_class"; Plural="CommissionSeatClasses"; Route="commission-seat-classes"; Fields=@("class", "description") },
    @{ Name="CommissionSeatStatus"; Table="commission_seat_status"; Plural="CommissionSeatStatuses"; Route="commission-seat-statuses"; Fields=@("status", "status_code", "description", "active") },
    @{ Name="CommissionerProfile"; Table="commissioner_profile"; Plural="CommissionerProfiles"; Route="commissioner-profiles"; Fields=@("oath", "invoices", "resides_in_service_area", "registered_voter", "recommended_for_appointment", "do_not_appoint", "do_not_appoint_details", "appointed_date", "assembly_meeting_date", "effective_date", "end_date", "commissioner_status_id", "commission_seat_id", "person_profile_id") },
    @{ Name="CommissionerStatus"; Table="commissioner_status"; Plural="CommissionerStatuses"; Route="commissioner-statuses"; Fields=@("status", "status_code", "description", "active") },
    @{ Name="PersonProfile"; Table="person_profile"; Plural="PersonProfiles"; Route="person-profiles"; Fields=@("active", "first_name", "last_name", "middle_name", "preferred_name", "person_honorific_id", "person_suffix_id", "auth_id") },
    @{ Name="PersonHonorific"; Table="person_honorific"; Plural="PersonHonorifics"; Route="person-honorifics"; Fields=@("honorific") },
    @{ Name="PersonSuffix"; Table="person_suffix"; Plural="PersonSuffixes"; Route="person-suffixes"; Fields=@("suffix") },
    @{ Name="PersonProfileVendor"; Table="person_profile_vendor"; Plural="PersonProfileVendors"; Route="person-profile-vendors"; Fields=@("title", "primary_contact", "active", "person_profile_id", "vendor_profile_id") },
    @{ Name="Road"; Table="road"; Plural="Roads"; Route="roads"; Fields=@("phase", "distance_feet", "start_description", "end_description", "start_coordinates", "end_coordinates", "approved_for_maintenance", "mpo", "legacy_subdivision_name", "road_name_id", "road_surface_type_id", "responder_code_id", "service_area_id") },
    @{ Name="RoadName"; Table="road_name"; Plural="RoadNames"; Route="road-names"; Fields=@("name", "match_feature_layer") },
    @{ Name="RoadSurfaceType"; Table="road_surface_type"; Plural="RoadSurfaceTypes"; Route="road-surface-types"; Fields=@("type") },
    @{ Name="RoadResponderCode"; Table="road_responder_code"; Plural="RoadResponderCodes"; Route="road-responder-codes"; Fields=@("code") },
    @{ Name="RoadSubdivision"; Table="road_subdivision"; Plural="RoadSubdivisions"; Route="road-subdivisions"; Fields=@("name", "feature_object_id") },
    @{ Name="VendorProfile"; Table="vendor_profile"; Plural="VendorProfiles"; Route="vendor-profiles"; Fields=@("active", "name", "doing_business_as", "vendor_id", "license_on_file", "license_expiration_date", "contractor_license", "contractor_license_expiration_date", "vendor_type_id", "vendor_vin_code_id") },
    @{ Name="VendorType"; Table="vendor_type"; Plural="VendorTypes"; Route="vendor-types"; Fields=@("type") },
    @{ Name="VendorVinCode"; Table="vendor_vin_code"; Plural="VendorVinCodes"; Route="vendor-vin-codes"; Fields=@("code", "description", "order") },
    @{ Name="Rfq"; Table="rfq"; Plural="Rfqs"; Route="rfqs"; Fields=@("rfq_number", "issue_date", "opening_date", "termination_date", "term_fy", "renewal_options", "custom_project_scope", "special_instructions", "completed", "bid_revised_date", "rebid_year", "rfq_project_scope_id", "rfq_ifb_type_id", "service_area_id") },
    @{ Name="RfqIfbType"; Table="rfq_ifb_type"; Plural="RfqIfbTypes"; Route="rfq-ifb-types"; Fields=@("type") },
    @{ Name="RfqProjectScope"; Table="rfq_project_scope"; Plural="RfqProjectScopes"; Route="rfq-project-scopes"; Fields=@("scope") },
    @{ Name="RfqVendorDistribution"; Table="rfq_vendor_distribution"; Plural="RfqVendorDistributions"; Route="rfq-vendor-distributions"; Fields=@("response_received", "declared_non_responsive", "awarded", "rfq_id", "vendor_profile_id", "person_profile_vendor_id") },
    @{ Name="Bid"; Table="bid"; Plural="Bids"; Route="bids"; Fields=@("bid_amount", "rfq_id", "vendor_profile_id", "specification_pay_item_id", "bid_quantity_id") },
    @{ Name="BidQuantity"; Table="bid_quantity"; Plural="BidQuantities"; Route="bid-quantities"; Fields=@("quantity", "rfq_id", "specification_pay_item_id") },
    @{ Name="SpecificationPayItem"; Table="specification_pay_item"; Plural="SpecificationPayItems"; Route="specification-pay-items"; Fields=@("pay_item_number", "pay_item", "pay_item_2", "original_published", "special_conditions", "conditions_description", "specification_pay_item_type_id", "specification_pay_unit_type_id") },
    @{ Name="SpecificationPayItemType"; Table="specification_pay_item_type"; Plural="SpecificationPayItemTypes"; Route="specification-pay-item-types"; Fields=@("code", "type") },
    @{ Name="SpecificationPayUnitType"; Table="specification_pay_unit_type"; Plural="SpecificationPayUnitTypes"; Route="specification-pay-unit-types"; Fields=@("unit", "description") },
    @{ Name="CorrespondenceAddress"; Table="correspondence_address"; Plural="CorrespondenceAddresses"; Route="correspondence-addresses"; Fields=@("address", "city", "state", "zip", "zip_ext", "country", "correspondence_address_type_id", "person_profile_id", "commissioner_profile_id", "vendor_profile_id") },
    @{ Name="CorrespondenceAddressType"; Table="correspondence_address_type"; Plural="CorrespondenceAddressTypes"; Route="correspondence-address-types"; Fields=@("type") },
    @{ Name="CorrespondenceEmail"; Table="correspondence_email"; Plural="CorrespondenceEmails"; Route="correspondence-emails"; Fields=@("email", "correspondence_email_type_id", "person_profile_id", "commissioner_profile_id", "vendor_profile_id") },
    @{ Name="CorrespondenceEmailType"; Table="correspondence_email_type"; Plural="CorrespondenceEmailTypes"; Route="correspondence-email-types"; Fields=@("type") },
    @{ Name="CorrespondencePhone"; Table="correspondence_phone"; Plural="CorrespondencePhones"; Route="correspondence-phones"; Fields=@("number", "correspondence_phone_type_id", "person_profile_id", "commissioner_profile_id", "vendor_profile_id") },
    @{ Name="CorrespondencePhoneType"; Table="correspondence_phone_type"; Plural="CorrespondencePhoneTypes"; Route="correspondence-phone-types"; Fields=@("type", "description") },
    @{ Name="CorrespondenceProfile"; Table="correspondence_profile"; Plural="CorrespondenceProfiles"; Route="correspondence-profiles"; Fields=@("phone", "email", "fax", "mailing_address", "public_profile", "person_profile_id", "commissioner_profile_id", "vendor_profile_id", "correspondence_phone_id", "correspondence_email_id", "correspondence_address_id", "correspondence_fax_id") }
)

function Convert-ToPascalCase {
    param([string]$text)
    return ($text -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

function Convert-ToCamelCase {
    param([string]$text)
    $pascal = Convert-ToPascalCase $text
    return $pascal.Substring(0,1).ToLower() + $pascal.Substring(1)
}

# Generate Service Files
foreach ($model in $models) {
    $servicePath = "AspireApp1WithAuth.ApiService\Services\$($model.Name)Service.cs"
    
    $fieldAssignments = ($model.Fields | ForEach-Object { 
        $pascalField = Convert-ToPascalCase $_
        "                    $_ = @$pascalField"
    }) -join ",`n"
    
    $updateFields = ($model.Fields | ForEach-Object { 
        $pascalField = Convert-ToPascalCase $_
        "                    $_ = @$pascalField"
    }) -join ",`n"
    
    $serviceContent = @"
using AspireApp1WithAuth.ApiService.Infrastructure;
using AspireApp1WithAuth.ApiService.Models;
using Dapper;
using StackExchange.Redis;
using System.Data;
using System.Text.Json;

namespace AspireApp1WithAuth.ApiService.Services;

public class $($model.Name)Service
{
    private readonly IDbConnection _db;
    private readonly IDatabase _cache;
    private const string CacheKeyPrefix = "$($model.Route.ToLower().Replace('-','')):";
    private const string HistoryCacheKeyPrefix = "$($model.Route.ToLower().Replace('-','')):history:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HistoryCacheExpiration = TimeSpan.FromHours(1);

    public $($model.Name)Service(IDbConnection db, IConnectionMultiplexer redis)
    {
        _db = db;
        _cache = redis.GetDatabase();
    }

    public async Task<Result<IEnumerable<$($model.Name)>>> GetAllAsync()
    {
        try
        {
            var cacheKey = `$"{CacheKeyPrefix}all";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<$($model.Name)>>(cached.ToString()!)!;
                return Result<IEnumerable<$($model.Name)>>.Success(cachedData);
            }

            var sql = "SELECT * FROM $($model.Table)";
            var result = await _db.QueryAsync<$($model.Name)>(sql);
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
            
            return Result<IEnumerable<$($model.Name)>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<$($model.Name)>>.Failure(`$"Failed to retrieve $($model.Plural.ToLower()): {ex.Message}");
        }
    }

    public async Task<Result<$($model.Name)>> GetByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<$($model.Name)>.Failure("Invalid ID provided");

            var cacheKey = `$"{CacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<$($model.Name)>(cached.ToString()!);
                return cachedData != null 
                    ? Result<$($model.Name)>.Success(cachedData)
                    : Result<$($model.Name)>.Failure("$($model.Name) not found");
            }

            var sql = "SELECT * FROM $($model.Table) WHERE id = @Id";
            var result = await _db.QueryFirstOrDefaultAsync<$($model.Name)>(sql, new { Id = id });
            
            if (result != null)
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), CacheExpiration);
                return Result<$($model.Name)>.Success(result);
            }
            
            return Result<$($model.Name)>.Failure("$($model.Name) not found");
        }
        catch (Exception ex)
        {
            return Result<$($model.Name)>.Failure(`$"Failed to retrieve $($model.Name.ToLower()): {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<$($model.Name)>>> GetHistoryByIdAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result<IEnumerable<$($model.Name)>>.Failure("Invalid ID provided");

            var cacheKey = `$"{HistoryCacheKeyPrefix}{id}";
            var cached = await _cache.StringGetAsync(cacheKey);
            
            if (cached.HasValue)
            {
                var cachedData = JsonSerializer.Deserialize<IEnumerable<$($model.Name)>>(cached.ToString()!)!;
                return Result<IEnumerable<$($model.Name)>>.Success(cachedData);
            }

            var sql = @"
                SELECT * FROM $($model.Table) 
                FOR SYSTEM_TIME ALL
                WHERE id = @Id
                ORDER BY ValidFrom DESC";
            
            var result = await _db.QueryAsync<$($model.Name)>(sql, new { Id = id });
            
            if (result.Any())
            {
                await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), HistoryCacheExpiration);
                return Result<IEnumerable<$($model.Name)>>.Success(result);
            }
            
            return Result<IEnumerable<$($model.Name)>>.Failure("No history found");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<$($model.Name)>>.Failure(`$"Failed to retrieve history: {ex.Message}");
        }
    }

    public async Task<Result<$($model.Name)>> CreateAsync($($model.Name) entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<$($model.Name)>.Failure(errors);
            }

            var sql = @"
                INSERT INTO $($model.Table) ($($model.Fields -join ', '))
                VALUES ($($model.Fields | ForEach-Object { "@$(Convert-ToPascalCase $_)" }) -join ', ');
                SELECT CAST(SCOPE_IDENTITY() as int)";

            entity.Id = await _db.ExecuteScalarAsync<int>(sql, entity);
            await InvalidateCacheAsync();
            
            return Result<$($model.Name)>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<$($model.Name)>.Failure(`$"Failed to create $($model.Name.ToLower()): {ex.Message}");
        }
    }

    public async Task<Result<$($model.Name)>> UpdateAsync($($model.Name) entity)
    {
        try
        {
            if (!ModelValidator.TryValidate(entity, out var validationResults))
            {
                var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error").ToArray();
                return Result<$($model.Name)>.Failure(errors);
            }

            var sql = @"
                UPDATE $($model.Table) SET
$($model.Fields | ForEach-Object { "                    $_ = @$(Convert-ToPascalCase $_)" } | Select-Object -First ($model.Fields.Count - 1) | ForEach-Object { "$_," })
                    $($model.Fields | Select-Object -Last 1 | ForEach-Object { "$_ = @$(Convert-ToPascalCase $_)" })
                WHERE id = @Id";

            var affected = await _db.ExecuteAsync(sql, entity);
            
            if (affected == 0)
                return Result<$($model.Name)>.Failure("$($model.Name) not found or no changes made");

            await InvalidateCacheAsync(entity);
            return Result<$($model.Name)>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<$($model.Name)>.Failure(`$"Failed to update $($model.Name.ToLower()): {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            if (id <= 0)
                return Result.Failure("Invalid ID provided");

            var sql = "DELETE FROM $($model.Table) WHERE id = @Id";
            var affected = await _db.ExecuteAsync(sql, new { Id = id });
            
            if (affected == 0)
                return Result.Failure("$($model.Name) not found");

            await InvalidateCacheAsync(deletedId: id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(`$"Failed to delete $($model.Name.ToLower()): {ex.Message}");
        }
    }

    private async Task InvalidateCacheAsync($($model.Name)? entity = null, int? deletedId = null)
    {
        await _cache.KeyDeleteAsync(`$"{CacheKeyPrefix}all");
        await _cache.KeyDeleteAsync(`$"{HistoryCacheKeyPrefix}all");
        
        if (entity != null)
        {
            await _cache.KeyDeleteAsync(`$"{CacheKeyPrefix}{entity.Id}");
            await _cache.KeyDeleteAsync(`$"{HistoryCacheKeyPrefix}{entity.Id}");
        }
        else if (deletedId.HasValue)
        {
            await _cache.KeyDeleteAsync(`$"{CacheKeyPrefix}{deletedId}");
            await _cache.KeyDeleteAsync(`$"{HistoryCacheKeyPrefix}{deletedId}");
        }
    }
}
"@

    Set-Content -Path $servicePath -Value $serviceContent
    Write-Host "Created: $servicePath" -ForegroundColor Green
}

Write-Host "`nAll service files generated successfully!" -ForegroundColor Cyan
Write-Host "Run this script again with '-Type Endpoints' or '-Type ApiClients' for those files" -ForegroundColor Yellow