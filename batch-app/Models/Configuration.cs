namespace BatchApp.Models;

public class BatchSettings
{
    public int ProcessIntervalMinutes { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 30;
    public bool EnableContinuousProcessing { get; set; } = true;
}

public class FileProcessingSettings
{
    // These directories are expected to be local mount paths (e.g., GCS bucket mounted at /data/in)
    public string InputDirectory { get; set; } = "/data/in";
    public string ProcessedDirectory { get; set; } = "/data/processed";
    public string FailedDirectory { get; set; } = "/data/failed";
    public string FilePattern { get; set; } = "*.csv";
    public bool DeleteAfterProcessing { get; set; } = false;
    public int MaxFilesPerBatch { get; set; } = 10;
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int BulkInsertBatchSize { get; set; } = 1000;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
}

public class ProcessedFileRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsFailed { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}

public class DataRecord
{
    public string? Column1 { get; set; }
    public string? Column2 { get; set; }
    public string? Column3 { get; set; }
    public string? Column4 { get; set; }
    public string? Column5 { get; set; }
    public DateTime ImportedAt { get; set; }
    public string ImportBatch { get; set; } = string.Empty;
}
