using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Key Vault in non-development environments
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = builder.Configuration["KeyVaultUrl"] 
        ?? throw new InvalidOperationException("KeyVaultUrl is required in production");
    
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl), 
        new DefaultAzureCredential());
    
    Console.WriteLine($"🔐 [AppHost] Loaded secrets from Key Vault: {keyVaultUrl}");
}

// Connection strings will come from Key Vault in production, User Secrets in dev
var authdb = builder.AddConnectionString("authdb");
var rsdb = builder.AddConnectionString("rsdb");

// Configure Redis
IResourceBuilder<IResourceWithConnectionString> redis;

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("🐳 [AppHost] Development mode: Creating local Redis container");
    redis = builder.AddRedis("cache");
}
else
{
    Console.WriteLine("☁️  [AppHost] Production mode: Using Azure Redis from Key Vault");
    redis = builder.AddConnectionString("cache");
}

var apiService = builder.AddProject<Projects.rs_ruralia_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(rsdb)
    .WithReference(redis);

builder.AddProject<Projects.rs_ruralia_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(authdb)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
