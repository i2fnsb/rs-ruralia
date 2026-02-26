# PowerShell Script to Generate All API Client Files
# Run this from the solution root directory

$models = @(
    @{ Name="CommissionSeatClass"; Table="commission_seat_class"; Plural="CommissionSeatClasses"; Route="commission-seat-classes" },
    @{ Name="CommissionSeatStatus"; Table="commission_seat_status"; Plural="CommissionSeatStatuses"; Route="commission-seat-statuses" },
    @{ Name="CommissionerProfile"; Table="commissioner_profile"; Plural="CommissionerProfiles"; Route="commissioner-profiles" },
    @{ Name="CommissionerStatus"; Table="commissioner_status"; Plural="CommissionerStatuses"; Route="commissioner-statuses" },
    @{ Name="PersonProfile"; Table="person_profile"; Plural="PersonProfiles"; Route="person-profiles" },
    @{ Name="PersonHonorific"; Table="person_honorific"; Plural="PersonHonorifics"; Route="person-honorifics" },
    @{ Name="PersonSuffix"; Table="person_suffix"; Plural="PersonSuffixes"; Route="person-suffixes" },
    @{ Name="PersonProfileVendor"; Table="person_profile_vendor"; Plural="PersonProfileVendors"; Route="person-profile-vendors" },
    @{ Name="Road"; Table="road"; Plural="Roads"; Route="roads" },
    @{ Name="RoadName"; Table="road_name"; Plural="RoadNames"; Route="road-names" },
    @{ Name="RoadSurfaceType"; Table="road_surface_type"; Plural="RoadSurfaceTypes"; Route="road-surface-types" },
    @{ Name="RoadResponderCode"; Table="road_responder_code"; Plural="RoadResponderCodes"; Route="road-responder-codes" },
    @{ Name="RoadSubdivision"; Table="road_subdivision"; Plural="RoadSubdivisions"; Route="road-subdivisions" },
    @{ Name="VendorProfile"; Table="vendor_profile"; Plural="VendorProfiles"; Route="vendor-profiles" },
    @{ Name="VendorType"; Table="vendor_type"; Plural="VendorTypes"; Route="vendor-types" },
    @{ Name="VendorVinCode"; Table="vendor_vin_code"; Plural="VendorVinCodes"; Route="vendor-vin-codes" },
    @{ Name="Rfq"; Table="rfq"; Plural="Rfqs"; Route="rfqs" },
    @{ Name="RfqIfbType"; Table="rfq_ifb_type"; Plural="RfqIfbTypes"; Route="rfq-ifb-types" },
    @{ Name="RfqProjectScope"; Table="rfq_project_scope"; Plural="RfqProjectScopes"; Route="rfq-project-scopes" },
    @{ Name="RfqVendorDistribution"; Table="rfq_vendor_distribution"; Plural="RfqVendorDistributions"; Route="rfq-vendor-distributions" },
    @{ Name="Bid"; Table="bid"; Plural="Bids"; Route="bids" },
    @{ Name="BidQuantity"; Table="bid_quantity"; Plural="BidQuantities"; Route="bid-quantities" },
    @{ Name="SpecificationPayItem"; Table="specification_pay_item"; Plural="SpecificationPayItems"; Route="specification-pay-items" },
    @{ Name="SpecificationPayItemType"; Table="specification_pay_item_type"; Plural="SpecificationPayItemTypes"; Route="specification-pay-item-types" },
    @{ Name="SpecificationPayUnitType"; Table="specification_pay_unit_type"; Plural="SpecificationPayUnitTypes"; Route="specification-pay-unit-types" },
    @{ Name="CorrespondenceAddress"; Table="correspondence_address"; Plural="CorrespondenceAddresses"; Route="correspondence-addresses" },
    @{ Name="CorrespondenceAddressType"; Table="correspondence_address_type"; Plural="CorrespondenceAddressTypes"; Route="correspondence-address-types" },
    @{ Name="CorrespondenceEmail"; Table="correspondence_email"; Plural="CorrespondenceEmails"; Route="correspondence-emails" },
    @{ Name="CorrespondenceEmailType"; Table="correspondence_email_type"; Plural="CorrespondenceEmailTypes"; Route="correspondence-email-types" },
    @{ Name="CorrespondencePhone"; Table="correspondence_phone"; Plural="CorrespondencePhones"; Route="correspondence-phones" },
    @{ Name="CorrespondencePhoneType"; Table="correspondence_phone_type"; Plural="CorrespondencePhoneTypes"; Route="correspondence-phone-types" },
    @{ Name="CorrespondenceProfile"; Table="correspondence_profile"; Plural="CorrespondenceProfiles"; Route="correspondence-profiles" }
)

# Generate API Client Files
foreach ($model in $models) {
    $apiClientPath = "AspireApp1WithAuth.Web\Services\$($model.Plural)ApiClient.cs"
    
    $apiClientContent = @"
using AspireApp1WithAuth.ApiService.Models;
using System.Net.Http.Json;

namespace AspireApp1WithAuth.Web.Services;

public class $($model.Plural)ApiClient(HttpClient httpClient)
{
    public async Task<Result<IEnumerable<$($model.Name)>>?> GetAll$($model.Plural)Async(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<$($model.Name)>>(
            () => httpClient.GetAsync("/$($model.Route)", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<$($model.Name)>?> Get$($model.Name)ByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<$($model.Name)>(
            () => httpClient.GetAsync(`$"/$($model.Route)/{id}", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<IEnumerable<$($model.Name)>>?> Get$($model.Name)HistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<IEnumerable<$($model.Name)>>(
            () => httpClient.GetAsync(`$"/$($model.Route)/{id}/history", cancellationToken),
            cancellationToken);
    }

    public async Task<Result<$($model.Name)>?> Create$($model.Name)Async($($model.Name) entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<$($model.Name)>(
            () => httpClient.PostAsJsonAsync("/$($model.Route)", entity, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<$($model.Name)>?> Update$($model.Name)Async(int id, $($model.Name) entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<$($model.Name)>(
            () => httpClient.PutAsJsonAsync(`$"/$($model.Route)/{id}", entity, cancellationToken),
            cancellationToken,
            successData: entity);
    }

    public async Task<Result?> Delete$($model.Name)Async(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            () => httpClient.DeleteAsync(`$"/$($model.Route)/{id}", cancellationToken),
            cancellationToken);
    }

    // Generic helper for requests that return data
    private async Task<Result<T>?> ExecuteAsync<T>(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken,
        T? successData = default)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                var data = successData ?? await response.Content.ReadFromJsonAsync<T>(cancellationToken);
                return data != null
                    ? Result<T>.Success(data)
                    : Result<T>.Failure("No data returned");
            }

            return await ParseErrorAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result<T>.Failure(`$"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(`$"Unexpected error: {ex.Message}");
        }
    }

    // Helper for requests that don't return data (like DELETE)
    private async Task<Result?> ExecuteAsync(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await request();

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return await ParseErrorAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(`$"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Unexpected error: {ex.Message}");
        }
    }

    // Parse error response for generic Result<T>
    private async Task<Result<T>> ParseErrorAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result<T>.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? `$"Request failed with status code {response.StatusCode}" });
    }

    // Parse error response for non-generic Result
    private async Task<Result> ParseErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken);
        return Result.Failure(
            errorResult?.Errors ?? new[] { errorResult?.Error ?? `$"Request failed with status code {response.StatusCode}" });
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string[]? Errors { get; set; }
    }
}
"@

    # Create directory if it doesn't exist
    $directory = Split-Path -Path $apiClientPath -Parent
    if (-not (Test-Path -Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -Path $apiClientPath -Value $apiClientContent
    Write-Host "Created: $apiClientPath" -ForegroundColor Green
}

Write-Host "`nAll API client files generated successfully!" -ForegroundColor Cyan
Write-Host "Total files created: $($models.Count)" -ForegroundColor Yellow