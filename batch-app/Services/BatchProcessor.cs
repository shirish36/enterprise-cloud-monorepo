using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using BatchApp.Models;
using BatchApp.Services;

namespace BatchApp.Services;

public class BatchProcessor : BackgroundService
{
    private readonly ILogger<BatchProcessor> _logger;
    private readonly BatchSettings _batchSettings;
    private readonly IFileProcessor _fileProcessor;

    public BatchProcessor(
        ILogger<BatchProcessor> logger,
        IOptions<BatchSettings> batchSettings,
        IFileProcessor fileProcessor)
    {
        _logger = logger;
        _batchSettings = batchSettings.Value;
        _fileProcessor = fileProcessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch processing service started at {Time}", DateTime.UtcNow);

        try
        {
            // Validate file processing configuration
            await _fileProcessor.ValidateConfigurationAsync();

            if (_batchSettings.EnableContinuousProcessing)
            {
                await RunContinuousProcessingAsync(stoppingToken);
            }
            else
            {
                await RunSingleBatchAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in batch processing service");
            throw;
        }
        finally
        {
            _logger.LogInformation("Batch processing service stopped at {Time}", DateTime.UtcNow);
        }
    }

    private async Task RunContinuousProcessingAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting continuous batch processing with {IntervalMinutes} minute intervals", 
            _batchSettings.ProcessIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync();
                
                // Wait for the specified interval before next processing
                var delay = TimeSpan.FromMinutes(_batchSettings.ProcessIntervalMinutes);
                _logger.LogInformation("Next batch processing scheduled in {DelayMinutes} minutes", delay.TotalMinutes);
                
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Batch processing was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during continuous batch processing");
                
                // Wait before retrying on error
                var retryDelay = TimeSpan.FromSeconds(_batchSettings.RetryDelaySeconds);
                _logger.LogInformation("Retrying in {RetryDelaySeconds} seconds due to error", retryDelay.TotalSeconds);
                
                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }

    private async Task RunSingleBatchAsync()
    {
        _logger.LogInformation("Running single batch processing");
        await ProcessBatchAsync();
        _logger.LogInformation("Single batch processing completed");
    }

    private async Task ProcessBatchAsync()
    {
        var batchId = GenerateBatchId();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting batch processing {BatchId} at {Time}", batchId, DateTime.UtcNow);

        try
        {
            // List all files in /data/in
            var inputDir = "/data/in";
            if (!Directory.Exists(inputDir))
            {
                _logger.LogWarning("Input directory does not exist: {InputDirectory}", inputDir);
                return;
            }
            var files = Directory.GetFiles(inputDir);
            _logger.LogInformation("Found {FileCount} files in {InputDirectory}", files.Length, inputDir);
            foreach (var file in files)
            {
                _logger.LogInformation("File: {FileName}", Path.GetFileName(file));
            }

            // If sample.txt exists, read and log its contents
            var sampleFile = Path.Combine(inputDir, "sample.txt");
            if (File.Exists(sampleFile))
            {
                _logger.LogInformation("Reading contents of sample.txt...");
                var content = await File.ReadAllTextAsync(sampleFile);
                _logger.LogInformation("Contents of sample.txt:\n{Content}", content);
            }
            else
            {
                _logger.LogInformation("sample.txt not found in {InputDirectory}", inputDir);
            }

            stopwatch.Stop();
            _logger.LogInformation("Batch processing {BatchId} completed in {Duration}ms", batchId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Critical error in batch processing {BatchId} after {Duration}ms", batchId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private string GenerateBatchId()
    {
        return $"BATCH_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
    }

    // No-op: DB and file move logic commented out for demo mode
}
