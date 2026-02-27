using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Configure connection to existing Azure SQL Database with Managed Identity
// The connection string should NOT include User ID or Password for managed identity
var authdb = builder.AddConnectionString("authdb");
var rsdb = builder.AddConnectionString("rsdb");

// Configure Redis based on environment
// Development: Create a local Redis container
// Production: Use Azure Managed Redis from connection string
IResourceBuilder<IResourceWithConnectionString> redis;

if (builder.Environment.IsDevelopment())
{
    // Local development - create Redis container
    Console.WriteLine("🐳 [AppHost] Development mode: Creating local Redis container");
    redis = builder.AddRedis("cache");
}
else
{
    // Production - use Azure Redis connection string
    var cacheConnectionString = builder.Configuration.GetConnectionString("cache");
    Console.WriteLine($"☁️  [AppHost] Production mode: Using Azure Redis at {cacheConnectionString?.Split(',')[0] ?? "NOT CONFIGURED"}");
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
