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
    private readonly IDatabaseService _databaseService;

    public BatchProcessor(
        ILogger<BatchProcessor> logger,
        IOptions<BatchSettings> batchSettings,
        IFileProcessor fileProcessor,
        IDatabaseService databaseService)
    {
        _logger = logger;
        _batchSettings = batchSettings.Value;
        _fileProcessor = fileProcessor;
        _databaseService = databaseService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch processing service started at {Time}", DateTime.UtcNow);

        try
        {
            // Initialize database schema
            await _databaseService.InitializeDatabaseAsync();

            // Test database connection
            if (!await _databaseService.TestConnectionAsync())
            {
                throw new InvalidOperationException("Cannot connect to database");
            }

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
            // Get files to process
            var filesToProcess = await _fileProcessor.GetFilesToProcessAsync();
            var filesList = filesToProcess.ToList();

            if (!filesList.Any())
            {
                _logger.LogInformation("No files found to process in batch {BatchId}", batchId);
                return;
            }

            _logger.LogInformation("Processing {FileCount} files in batch {BatchId}", filesList.Count, batchId);

            var totalProcessed = 0;
            var totalFailed = 0;
            var successfulFiles = 0;
            var failedFiles = 0;

            // Process each file
            foreach (var filePath in filesList)
            {
                var fileName = Path.GetFileName(filePath);
                var fileStopwatch = Stopwatch.StartNew();
                
                try
                {
                    _logger.LogInformation("Processing file: {FileName}", fileName);

                    // Create initial processed file record
                    var fileInfo = new FileInfo(filePath);
                    var processedFileRecord = new ProcessedFileRecord
                    {
                        FileName = fileName,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        ProcessedAt = DateTime.UtcNow,
                        Status = "Processing"
                    };

                    await _databaseService.InsertProcessedFileRecordAsync(processedFileRecord);

                    // Process the file
                    var (processed, failed) = await _fileProcessor.ProcessFileAsync(filePath, batchId);
                    
                    fileStopwatch.Stop();
                    
                    // Update statistics
                    totalProcessed += processed;
                    totalFailed += failed;

                    if (failed == 0)
                    {
                        successfulFiles++;
                        
                        // Move file to processed directory
                        await _fileProcessor.MoveFileAsync(filePath, GetProcessedDirectory());
                        
                        // Update database record
                        await _databaseService.UpdateProcessedFileRecordAsync(
                            processedFileRecord.Id, processed, failed, "Completed", null, fileStopwatch.Elapsed);
                        
                        _logger.LogInformation("Successfully processed file: {FileName} ({Processed} records) in {Duration}ms", 
                            fileName, processed, fileStopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        failedFiles++;
                        
                        // Move file to failed directory
                        await _fileProcessor.MoveFileAsync(filePath, GetFailedDirectory());
                        
                        var errorMessage = $"Processing completed with {failed} failed records out of {processed + failed} total records";
                        await _databaseService.UpdateProcessedFileRecordAsync(
                            processedFileRecord.Id, processed, failed, "CompletedWithErrors", errorMessage, fileStopwatch.Elapsed);
                        
                        _logger.LogWarning("File processed with errors: {FileName} ({Processed} successful, {Failed} failed) in {Duration}ms", 
                            fileName, processed, failed, fileStopwatch.ElapsedMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    fileStopwatch.Stop();
                    
                    _logger.LogError(ex, "Failed to process file: {FileName} in {Duration}ms", fileName, fileStopwatch.ElapsedMilliseconds);
                    
                    try
                    {
                        // Move file to failed directory
                        await _fileProcessor.MoveFileAsync(filePath, GetFailedDirectory());
                        
                        // Update database record if possible
                        var errorMessage = $"Processing failed: {ex.Message}";
                        // Note: We'd need the record ID here - could be improved by returning it from the insert
                    }
                    catch (Exception moveEx)
                    {
                        _logger.LogError(moveEx, "Failed to move failed file: {FileName}", fileName);
                    }
                }
            }

            stopwatch.Stop();

            // Log batch completion summary
            _logger.LogInformation(
                "Batch processing {BatchId} completed in {Duration}ms. " +
                "Files: {SuccessfulFiles} successful, {FailedFiles} failed. " +
                "Records: {TotalProcessed} processed, {TotalFailed} failed",
                batchId, stopwatch.ElapsedMilliseconds, successfulFiles, failedFiles, totalProcessed, totalFailed);

            // Additional metrics logging for monitoring
            _logger.LogInformation("METRIC: BatchProcessingTime={Duration}ms BatchId={BatchId}", stopwatch.ElapsedMilliseconds, batchId);
            _logger.LogInformation("METRIC: FilesProcessed={Count} BatchId={BatchId}", successfulFiles, batchId);
            _logger.LogInformation("METRIC: FilesFailed={Count} BatchId={BatchId}", failedFiles, batchId);
            _logger.LogInformation("METRIC: RecordsProcessed={Count} BatchId={BatchId}", totalProcessed, batchId);
            _logger.LogInformation("METRIC: RecordsFailed={Count} BatchId={BatchId}", totalFailed, batchId);
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

    private string GetProcessedDirectory()
    {
        return Environment.GetEnvironmentVariable("PROCESSED_DIRECTORY") ?? "/mnt/gcs-bucket/processed";
    }

    private string GetFailedDirectory()
    {
        return Environment.GetEnvironmentVariable("FAILED_DIRECTORY") ?? "/mnt/gcs-bucket/failed";
    }
}
