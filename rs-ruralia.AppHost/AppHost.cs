var builder = DistributedApplication.CreateBuilder(args);

// Configure connection to existing Azure SQL Database with Managed Identity
// The connection string should NOT include User ID or Password for managed identity
var authdb = builder.AddConnectionString("authdb");
var rsdb = builder.AddConnectionString("rsdb");

// shared Redis instance
var redis = builder.AddRedis("cache");

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
