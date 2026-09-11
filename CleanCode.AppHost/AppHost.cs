using System.Reflection.Metadata;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CleanCode_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var clientParameter = builder.AddParameter("GoogleClientId", secret:true );
var clientsecretParameter = builder.AddParameter("GoogleClientSecret", secret: true );
builder.AddProject<Projects.CleanCode_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithEnvironment("Google:ClientId", clientParameter)
    .WithEnvironment("Google:ClientSecret", clientsecretParameter)
    .WaitFor(apiService);

builder.Build().Run();