using rs_ruralia.Web;
using rs_ruralia.Web.Components;
using rs_ruralia.Web.Components.Account;
using rs_ruralia.Web.Data;
using rs_ruralia.Web.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using rs_ruralia.Web.Components.Account.Endpoints;
using Microsoft.Data.SqlClient;
using MudBlazor.Services;
using rs_ruralia.Web.Services;
using Microsoft.Extensions.Hosting;
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
    
    Console.WriteLine($"🔐 [Web] Loaded secrets from Key Vault: {keyVaultUrl}");
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add authentication and authorization
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Register SQL connection factory for Dapper (authdb - used for Identity)
builder.Services.AddScoped<System.Data.IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("authdb")
        ?? throw new InvalidOperationException("Connection string 'authdb' not found.");
    
    return new SqlConnection(connectionString);
});

// Configure Identity with Dapper-based stores
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddUserStore<DapperUserStore>()
    .AddRoleStore<DapperRoleStore>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.AdminOnly, policy =>
        policy.Requirements.Add(new MinimumRoleRequirement(Roles.AdminAndAbove)));
    options.AddPolicy(Policies.FnsbUserOrAbove, policy =>
        policy.Requirements.Add(new MinimumRoleRequirement(Roles.FnsbUserAndAbove)));
    options.AddPolicy(Policies.CommissionerOrAbove, policy =>
        policy.Requirements.Add(new MinimumRoleRequirement(Roles.CommissionerAndAbove)));
    options.AddPolicy(Policies.VendorOrAbove, policy =>
        policy.Requirements.Add(new MinimumRoleRequirement(Roles.VendorAndAbove)));
    options.AddPolicy(Policies.AuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());
});

builder.Services.AddSingleton<IAuthorizationHandler, MinimumRoleHandler>();
builder.Services.AddScoped<DapperRepository>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();

builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

// Register ViewModeService
builder.Services.AddScoped<ViewModeService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Service Area API Clients
builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });
builder.Services.AddHttpClient<ServiceAreasApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });
builder.Services.AddHttpClient<ServiceAreaCodesApiClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    });

// Ordinance API Clients
builder.Services.AddHttpClient<OrdinancesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<OrdinanceTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Note API Client
builder.Services.AddHttpClient<NotesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Commission API Clients
builder.Services.AddHttpClient<CommissionSeatsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CommissionSeatTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CommissionSeatClassesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CommissionSeatStatusesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CommissionerProfilesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CommissionerStatusesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Person API Clients
builder.Services.AddHttpClient<PersonProfilesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<PersonHonorificsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<PersonSuffixesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<PersonProfileVendorsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Road API Clients
builder.Services.AddHttpClient<RoadsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RoadNamesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RoadSurfaceTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RoadResponderCodesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RoadSubdivisionsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Vendor API Clients
builder.Services.AddHttpClient<VendorProfilesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<VendorTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<VendorVinCodesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// RFQ API Clients
builder.Services.AddHttpClient<RfqsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RfqIfbTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RfqProjectScopesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<RfqVendorDistributionsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Bid and Specification API Clients
builder.Services.AddHttpClient<BidsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<BidQuantitiesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<SpecificationPayItemsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<SpecificationPayItemTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<SpecificationPayUnitTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

// Correspondence API Clients
builder.Services.AddHttpClient<CorrespondenceAddressesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondenceAddressTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondenceEmailsApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondenceEmailTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondencePhonesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondencePhoneTypesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});
builder.Services.AddHttpClient<CorrespondenceProfilesApiClient>(client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();

// Register Lookup Data Service (with caching)
builder.Services.AddScoped<ILookupDataService, LookupDataService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
app.MapDefaultEndpoints();

app.MapGet("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager, HttpContext context) =>
{
    try
    {
        await signInManager.SignOutAsync();
    }
    catch
    {
        // Optionally log; swallow to ensure redirect proceeds
    }
    
    // Get return URL from query string if provided
    var returnUrl = context.Request.Query["ReturnUrl"].ToString();
    var targetUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    
    // Force a full page reload to destroy the Blazor circuit
    return Results.Content(
        $"<script>window.location.href = '{targetUrl}';</script>",
        "text/html");
});

app.Run();
