using rs_ruralia.ApiService.Endpoints;
using rs_ruralia.Shared.Infrastructure;
using rs_ruralia.Shared.Models;
using rs_ruralia.ApiService.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault in non-development environments
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"] 
        ?? throw new InvalidOperationException("KeyVaultUrl is required in production");
    
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl), 
        new DefaultAzureCredential());
    
    Console.WriteLine($"🔐 [ApiService] Loaded secrets from Key Vault: {keyVaultUrl}");
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add SQL Server connection for rsdb
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("rsdb")
        ?? throw new InvalidOperationException("Connection string 'rsdb' not found.");
    return new SqlConnection(connectionString);
});

// Add Redis using Aspire integration
var cacheConnectionString = builder.Configuration.GetConnectionString("cache");
if (!string.IsNullOrEmpty(cacheConnectionString))
{
    var endpoint = cacheConnectionString.Split(',')[0];
    var isLocal = endpoint.Contains("localhost") || endpoint.Contains("127.0.0.1");
    Console.WriteLine(isLocal 
        ? $"🔌 [ApiService] Connected to LOCAL Redis: {endpoint}"
        : $"☁️  [ApiService] Connected to AZURE Redis: {endpoint}");
}
builder.AddRedisClient("cache");

// Register services as scoped (one instance per request)
// Service Area Services
builder.Services.AddScoped<ServiceAreaService>();
builder.Services.AddScoped<ServiceAreaCodeService>();

// Note Service
builder.Services.AddScoped<NoteService>();

// Ordinance Services
builder.Services.AddScoped<OrdinanceService>();
builder.Services.AddScoped<OrdinanceTypeService>();

// Commission Services
builder.Services.AddScoped<CommissionSeatService>();
builder.Services.AddScoped<CommissionSeatTypeService>();
builder.Services.AddScoped<CommissionSeatClassService>();
builder.Services.AddScoped<CommissionSeatStatusService>();
builder.Services.AddScoped<CommissionerProfileService>();
builder.Services.AddScoped<CommissionerStatusService>();

// Person Services
builder.Services.AddScoped<PersonProfileService>();
builder.Services.AddScoped<PersonHonorificService>();
builder.Services.AddScoped<PersonSuffixService>();
builder.Services.AddScoped<PersonProfileVendorService>();

// Road Services
builder.Services.AddScoped<RoadService>();
builder.Services.AddScoped<RoadNameService>();
builder.Services.AddScoped<RoadSurfaceTypeService>();
builder.Services.AddScoped<RoadResponderCodeService>();
builder.Services.AddScoped<RoadSubdivisionService>();

// Vendor Services
builder.Services.AddScoped<VendorProfileService>();
builder.Services.AddScoped<VendorTypeService>();
builder.Services.AddScoped<VendorVinCodeService>();

// RFQ Services
builder.Services.AddScoped<RfqService>();
builder.Services.AddScoped<RfqIfbTypeService>();
builder.Services.AddScoped<RfqProjectScopeService>();
builder.Services.AddScoped<RfqVendorDistributionService>();

// Bid and Specification Services
builder.Services.AddScoped<BidService>();
builder.Services.AddScoped<BidQuantityService>();
builder.Services.AddScoped<SpecificationPayItemService>();
builder.Services.AddScoped<SpecificationPayItemTypeService>();
builder.Services.AddScoped<SpecificationPayUnitTypeService>();

// Correspondence Services
builder.Services.AddScoped<CorrespondenceAddressService>();
builder.Services.AddScoped<CorrespondenceAddressTypeService>();
builder.Services.AddScoped<CorrespondenceEmailService>();
builder.Services.AddScoped<CorrespondenceEmailTypeService>();
builder.Services.AddScoped<CorrespondencePhoneService>();
builder.Services.AddScoped<CorrespondencePhoneTypeService>();
builder.Services.AddScoped<CorrespondenceProfileService>();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Configure JSON options to convert empty strings to null
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new EmptyStringToNullConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "RSDB API Service - Available endpoints: /service-areas, /ordinances, /commission-seats, /roads, /vendors, /rfqs");

// Configure Dapper to use [Column] attributes for all models
ConfigureDapperColumnMapping(
    // Service Area
    typeof(ServiceArea), typeof(ServiceAreaCode),
    // Ordinances
    typeof(Ordinance), typeof(OrdinanceType),
    // Notes
    typeof(Note),
    // Commission
    typeof(CommissionSeat), typeof(CommissionSeatType), typeof(CommissionSeatClass), typeof(CommissionSeatStatus),
    typeof(CommissionerProfile), typeof(CommissionerStatus),
    // Person
    typeof(PersonProfile), typeof(PersonHonorific), typeof(PersonSuffix), typeof(PersonProfileVendor),
    // Road
    typeof(Road), typeof(RoadName), typeof(RoadSurfaceType), typeof(RoadResponderCode),
    typeof(RoadSubdivision), typeof(RoadRoadSubdivision),
    // Vendor
    typeof(VendorProfile), typeof(VendorType), typeof(VendorVinCode),
    // RFQ
    typeof(Rfq), typeof(RfqIfbType), typeof(RfqProjectScope), typeof(RfqVendorDistribution),
    // Bid and Specification
    typeof(Bid), typeof(BidQuantity), typeof(SpecificationPayItem),
    typeof(SpecificationPayItemType), typeof(SpecificationPayUnitType),
    // Correspondence
    typeof(CorrespondenceAddress), typeof(CorrespondenceAddressType),
    typeof(CorrespondenceEmail), typeof(CorrespondenceEmailType),
    typeof(CorrespondencePhone), typeof(CorrespondencePhoneType),
    typeof(CorrespondenceProfile)
);

// Map all endpoints
app.MapServiceAreaEndpoints();
app.MapServiceAreaCodeEndpoints();
app.MapNoteEndpoints();
app.MapOrdinanceEndpoints();
app.MapOrdinanceTypeEndpoints();

// Commission endpoints
app.MapCommissionSeatEndpoints();
app.MapCommissionSeatTypeEndpoints();
app.MapCommissionSeatClassEndpoints();
app.MapCommissionSeatStatusEndpoints();
app.MapCommissionerProfileEndpoints();
app.MapCommissionerStatusEndpoints();

// Person endpoints
app.MapPersonProfileEndpoints();
app.MapPersonHonorificEndpoints();
app.MapPersonSuffixEndpoints();
app.MapPersonProfileVendorEndpoints();

// Road endpoints
app.MapRoadEndpoints();
app.MapRoadNameEndpoints();
app.MapRoadSurfaceTypeEndpoints();
app.MapRoadResponderCodeEndpoints();
app.MapRoadSubdivisionEndpoints();

// Vendor endpoints
app.MapVendorProfileEndpoints();
app.MapVendorTypeEndpoints();
app.MapVendorVinCodeEndpoints();

// RFQ endpoints
app.MapRfqEndpoints();
app.MapRfqIfbTypeEndpoints();
app.MapRfqProjectScopeEndpoints();
app.MapRfqVendorDistributionEndpoints();

// Bid and Specification endpoints
app.MapBidEndpoints();
app.MapBidQuantityEndpoints();
app.MapSpecificationPayItemEndpoints();
app.MapSpecificationPayItemTypeEndpoints();
app.MapSpecificationPayUnitTypeEndpoints();

// Correspondence endpoints
app.MapCorrespondenceAddressEndpoints();
app.MapCorrespondenceAddressTypeEndpoints();
app.MapCorrespondenceEmailEndpoints();
app.MapCorrespondenceEmailTypeEndpoints();
app.MapCorrespondencePhoneEndpoints();
app.MapCorrespondencePhoneTypeEndpoints();
app.MapCorrespondenceProfileEndpoints();

app.MapDefaultEndpoints();

app.Run();

static void ConfigureDapperColumnMapping(params Type[] types)
{
    foreach (var type in types)
    {
        SqlMapper.SetTypeMap(type, new CustomPropertyTypeMap(
            type,
            (t, columnName) =>
                t.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName)
                ) ?? t.GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)!
        ));
    }
}
