# PowerShell Script to Generate All Endpoint Files
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

# Generate Endpoint Files
foreach ($model in $models) {
    $endpointPath = "AspireApp1WithAuth.ApiService\Endpoints\$($model.Name)Endpoints.cs"
    
    $endpointContent = @"
using AspireApp1WithAuth.ApiService.Models;
using AspireApp1WithAuth.ApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1WithAuth.ApiService.Endpoints;

public static class $($model.Name)Endpoints
{
    public static IEndpointRouteBuilder Map$($model.Name)Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/$($model.Route)")
            .WithTags("$($model.Plural)");

        group.MapGet("/", async ($($model.Name)Service service) =>
        {
            var result = await service.GetAllAsync();
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Get$($model.Plural)");

        group.MapGet("/{id:int}", async (int id, $($model.Name)Service service) =>
        {
            var result = await service.GetByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Get$($model.Name)");

        group.MapGet("/{id:int}/history", async (int id, $($model.Name)Service service) =>
        {
            var result = await service.GetHistoryByIdAsync(id);
            return result.IsSuccess 
                ? Results.Ok(result.Data) 
                : Results.NotFound(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Get$($model.Name)History");

        group.MapPost("/", async ($($model.Name) entity, $($model.Name)Service service) =>
        {
            var result = await service.CreateAsync(entity);
            return result.IsSuccess 
                ? Results.Created(`$"/$($model.Route)/{result.Data!.Id}", result.Data)
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Create$($model.Name)");

        group.MapPut("/{id:int}", async (int id, $($model.Name) entity, $($model.Name)Service service) =>
        {
            entity.Id = id;
            var result = await service.UpdateAsync(entity);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Update$($model.Name)");

        group.MapDelete("/{id:int}", async (int id, $($model.Name)Service service) =>
        {
            var result = await service.DeleteAsync(id);
            return result.IsSuccess 
                ? Results.NoContent() 
                : Results.BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
        })
        .WithName("Delete$($model.Name)");

        return app;
    }
}
"@

    # Create directory if it doesn't exist
    $directory = Split-Path -Path $endpointPath -Parent
    if (-not (Test-Path -Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Set-Content -Path $endpointPath -Value $endpointContent
    Write-Host "Created: $endpointPath" -ForegroundColor Green
}

Write-Host "`nAll endpoint files generated successfully!" -ForegroundColor Cyan
Write-Host "Total files created: $($models.Count)" -ForegroundColor Yellow