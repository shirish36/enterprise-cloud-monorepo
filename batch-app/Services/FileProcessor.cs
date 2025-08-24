using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using BatchApp.Models;

namespace BatchApp.Services;

public interface IFileProcessor
{
    Task ValidateConfigurationAsync();
    Task<IEnumerable<string>> GetFilesToProcessAsync();
    Task<(int processed, int failed)> ProcessFileAsync(string filePath, string batchId);
    Task MoveFileAsync(string sourceFilePath, string destinationDirectory);
    Task DeleteFileAsync(string filePath);
    Task LogAllFileContentsAsync();
}

public class FileProcessor : IFileProcessor
{
    // All file I/O is performed on local directories. GCS bucket must be mounted at /data/in by Cloud Run.
    private readonly FileProcessingSettings _settings;
    private readonly ILogger<FileProcessor> _logger;
    private readonly string _inputDirectory;
    // Removed processed and failed directory references

    public FileProcessor(IOptions<FileProcessingSettings> settings, ILogger<FileProcessor> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _inputDirectory = _settings.InputDirectory;
    // Removed initialization of processed and failed directories
    }

    public Task ValidateConfigurationAsync()
    {
        _logger.LogInformation("Validating file processing configuration...");
        if (!Directory.Exists(_inputDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory does not exist: {_inputDirectory}");
        }
        // No need to create or check permissions for processed/failed directories; these are managed by the GCS mount.
        _logger.LogInformation("File processing configuration validated successfully");
        _logger.LogInformation("Input Directory: {InputDirectory}", _inputDirectory);
    // Removed logging for processed and failed directories and file pattern
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetFilesToProcessAsync()
    {
        // Reads files from the local mount path (e.g., /data/in) as provided by Cloud Run GCS volume mount
        try
        {
            var files = Directory.GetFiles(_inputDirectory);
            _logger.LogInformation("Found {FileCount} files to process in {Directory}", files.Length, _inputDirectory);
            _logger.LogInformation("Found {FileCount} files to process in {Directory}", files.Count, _inputDirectory);
            foreach (var file in files)
            {
                _logger.LogDebug("File to process: {FileName} ({FileSize} bytes)", Path.GetFileName(file), new FileInfo(file).Length);
            }
            return Task.FromResult<IEnumerable<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning input directory: {Directory}", _inputDirectory);
            return Task.FromResult<IEnumerable<string>>(Enumerable.Empty<string>());
        }
    }

    public async Task<(int processed, int failed)> ProcessFileAsync(string filePath, string batchId)
    {
        var fileName = Path.GetFileName(filePath);
        _logger.LogInformation("Processing file: {FileName}", fileName);

        var processed = 0;
        var failed = 0;

        try
        {
            // Determine file type and process accordingly
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            switch (extension)
            {
                case ".csv":
                    (processed, failed) = await ProcessCsvFileAsync(filePath, batchId);
                    break;
                case ".txt":
                    (processed, failed) = await ProcessTextFileAsync(filePath, batchId);
                    break;
                case ".json":
                    // Removed ProcessJsonFileAsync call; not needed for current requirements
                    break;
                default:
                    _logger.LogWarning("Unsupported file type: {Extension} for file: {FileName}", extension, fileName);
                    failed = 1;
                    break;
            }

            _logger.LogInformation("Completed processing file: {FileName}. Processed: {Processed}, Failed: {Failed}", 
                fileName, processed, failed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FileName}", fileName);
            failed = 1;
        }

        return (processed, failed);
    }

    private async Task<(int processed, int failed)> ProcessCsvFileAsync(string filePath, string batchId)
    {
        var processed = 0;
        var failed = 0;
        var records = new List<DataRecord>();

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        });

        try
        {
            // Read header
            csv.Read();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;
            
            _logger.LogDebug("CSV Headers: {Headers}", string.Join(", ", headers ?? new string[0]));

            // Read data rows
            while (csv.Read())
            {
                try
                {
                    var record = new DataRecord
                    {
                        Column1 = csv.GetField(0),
                        Column2 = csv.GetField(1),
                        Column3 = csv.GetField(2),
                        Column4 = csv.GetField(3),
                        Column5 = csv.GetField(4),
                        ImportedAt = DateTime.UtcNow,
                        ImportBatch = batchId
                    };

                    records.Add(record);
                    processed++;

                    // Process in batches to avoid memory issues
                    if (records.Count >= 1000)
                    {
                        await ProcessRecordBatch(records, batchId);
                        records.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process CSV row {RowNumber} in file: {FileName}", 
                        csv.CurrentIndex, Path.GetFileName(filePath));
                    failed++;
                }
            }

            // Process remaining records
            if (records.Any())
            {
                await ProcessRecordBatch(records, batchId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file: {FileName}", Path.GetFileName(filePath));
            throw;
        }

        return (processed, failed);
    }

    private async Task<(int processed, int failed)> ProcessTextFileAsync(string filePath, string batchId)
    {
        var processed = 0;
        var failed = 0;
        var records = new List<DataRecord>();

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            
            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                try
                {
                    // Assume pipe-delimited or tab-delimited text file
                    var delimiter = line.Contains('\t') ? '\t' : '|';
                    var fields = line.Split(delimiter);

                    var record = new DataRecord
                    {
                        Column1 = fields.Length > 0 ? fields[0] : null,
                        Column2 = fields.Length > 1 ? fields[1] : null,
                        Column3 = fields.Length > 2 ? fields[2] : null,
                        Column4 = fields.Length > 3 ? fields[3] : null,
                        Column5 = fields.Length > 4 ? fields[4] : null,
                        ImportedAt = DateTime.UtcNow,
                        ImportBatch = batchId
                    };

                    records.Add(record);
                    processed++;

                    if (records.Count >= 1000)
                    {
                        await ProcessRecordBatch(records, batchId);
                        records.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process text line in file: {FileName}", Path.GetFileName(filePath));
                    failed++;
                }
            }

            if (records.Any())
            {
                await ProcessRecordBatch(records, batchId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading text file: {FileName}", Path.GetFileName(filePath));
            throw;
        }

        return (processed, failed);
    }

    public async Task LogAllFileContentsAsync()
    {
        try
        {
            var files = Directory.GetFiles(_inputDirectory);
            foreach (var file in files)
            {
                _logger.LogInformation("Reading file: {FileName}", Path.GetFileName(file));
                var content = await File.ReadAllTextAsync(file);
                _logger.LogInformation("Contents of {FileName}:\n{Content}", Path.GetFileName(file), content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading files in {Directory}", _inputDirectory);
        }
    }

    private async Task ProcessRecordBatch(List<DataRecord> records, string batchId)
    {
        // This would typically call the database service
        // For now, we'll just log the batch processing
        _logger.LogDebug("Processing batch of {RecordCount} records for batch {BatchId}", records.Count, batchId);
        
        // Simulate processing time
        await Task.Delay(100);
    }

    public Task MoveFileAsync(string sourceFilePath, string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }
        var fileName = Path.GetFileName(sourceFilePath);
        var destinationFilePath = Path.Combine(destinationDirectory, fileName);
        if (File.Exists(destinationFilePath))
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            fileName = $"{nameWithoutExt}_{timestamp}{extension}";
            destinationFilePath = Path.Combine(destinationDirectory, fileName);
        }
        File.Move(sourceFilePath, destinationFilePath);
        _logger.LogInformation("Moved file from {Source} to {Destination}", sourceFilePath, destinationFilePath);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted file: {FilePath}", filePath);
        }
        return Task.CompletedTask;
    }

}
