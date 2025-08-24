using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Google.Cloud.Logging.V2;
using Google.Cloud.Diagnostics.AspNetCore;
using BatchApp.Services;
using BatchApp.Models;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure services
builder.Services.Configure<BatchSettings>(builder.Configuration.GetSection("BatchSettings"));
builder.Services.Configure<FileProcessingSettings>(builder.Configuration.GetSection("FileProcessing"));
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));

builder.Services.AddSingleton<GcsFileService>(sp => {
    var settings = sp.GetRequiredService<IOptions<FileProcessingSettings>>().Value;
    var logger = sp.GetRequiredService<ILogger<GcsFileService>>();
    var bucketName = Environment.GetEnvironmentVariable("BUCKET_NAME") ?? settings.BucketName;
    return new GcsFileService(bucketName, logger);
});
builder.Services.AddSingleton<IFileProcessor, FileProcessor>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddHostedService<BatchProcessor>();


var host = builder.Build();

// Add Google Cloud Logging to ILoggerFactory after building the host
var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
var serviceProvider = host.Services;
var projectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT") ?? builder.Configuration["GoogleCloud:ProjectId"];
if (!string.IsNullOrEmpty(projectId))
{
    loggerFactory.AddGoogle(serviceProvider, projectId);
}

try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Batch Processing Application");
    
    // Validate configuration on startup
    var configValidator = host.Services.GetRequiredService<IFileProcessor>();
    await ((FileProcessor)configValidator).ValidateConfigurationAsync();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Batch Processing Application stopped");
}
