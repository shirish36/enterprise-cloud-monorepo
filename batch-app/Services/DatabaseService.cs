using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using BatchApp.Models;

namespace BatchApp.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    Task<bool> TestConnectionAsync();
    Task InsertDataRecordsAsync(IEnumerable<DataRecord> records);
    Task InsertProcessedFileRecordAsync(ProcessedFileRecord record);
    Task UpdateProcessedFileRecordAsync(int recordId, int recordsProcessed, int recordsFailed, string status, string? errorMessage, TimeSpan duration);
}

public class DatabaseService : IDatabaseService
{
    private readonly DatabaseSettings _settings;
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;

    public DatabaseService(IOptions<DatabaseSettings> settings, ILogger<DatabaseService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        // Get connection string from environment variable or configuration
        _connectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING") ?? _settings.ConnectionString;
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured. Set SQL_CONNECTION_STRING environment variable or Database:ConnectionString in configuration.");
        }
        // Log the DB connection string with password masked
        if (!string.IsNullOrEmpty(_connectionString))
        {
            var masked = MaskPassword(_connectionString);
            _logger.LogInformation("Database connection string: {ConnectionString}", masked);
        }
    }

    private string MaskPassword(string connStr)
    {
        // Simple mask for Password=...; or pwd=...;
        return System.Text.RegularExpressions.Regex.Replace(
            connStr,
            @"(Password|pwd)=[^;]*",
            "$1=******",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing database schema...");
        
        var createTablesScript = @"
            -- Create ProcessedFiles table if it doesn't exist
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessedFiles' AND xtype='U')
            CREATE TABLE ProcessedFiles (
                Id int IDENTITY(1,1) PRIMARY KEY,
                FileName nvarchar(255) NOT NULL,
                FilePath nvarchar(500) NOT NULL,
                FileSize bigint NOT NULL,
                ProcessedAt datetime2 NOT NULL,
                RecordsProcessed int NOT NULL DEFAULT 0,
                RecordsFailed int NOT NULL DEFAULT 0,
                Status nvarchar(50) NOT NULL,
                ErrorMessage nvarchar(max) NULL,
                ProcessingDurationMs bigint NOT NULL DEFAULT 0,
                CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
            );

            -- Create DataRecords table if it doesn't exist
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DataRecords' AND xtype='U')
            CREATE TABLE DataRecords (
                Id bigint IDENTITY(1,1) PRIMARY KEY,
                Column1 nvarchar(255) NULL,
                Column2 nvarchar(255) NULL,
                Column3 nvarchar(255) NULL,
                Column4 nvarchar(255) NULL,
                Column5 nvarchar(255) NULL,
                ImportedAt datetime2 NOT NULL,
                ImportBatch nvarchar(100) NOT NULL,
                CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
            );

            -- Create indexes for better performance
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_ProcessedFiles_ProcessedAt')
            CREATE INDEX IX_ProcessedFiles_ProcessedAt ON ProcessedFiles (ProcessedAt);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_DataRecords_ImportBatch')
            CREATE INDEX IX_DataRecords_ImportBatch ON DataRecords (ImportBatch);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_DataRecords_ImportedAt')
            CREATE INDEX IX_DataRecords_ImportedAt ON DataRecords (ImportedAt);
        ";

        await ExecuteWithRetryAsync(async () =>
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand(createTablesScript, connection);
            command.CommandTimeout = _settings.CommandTimeout;
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Database schema initialized successfully");
        });
    }

    public async Task InsertDataRecordsAsync(IEnumerable<DataRecord> records)
    {
        var recordsList = records.ToList();
        if (!recordsList.Any())
        {
            _logger.LogWarning("No records to insert");
            return;
        }

        _logger.LogInformation("Inserting {RecordCount} data records", recordsList.Count);

        await ExecuteWithRetryAsync(async () =>
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Use bulk insert for better performance
            var batches = recordsList.Chunk(_settings.BulkInsertBatchSize);
            var totalInserted = 0;

            foreach (var batch in batches)
            {
                using var transaction = connection.BeginTransaction();
                try
                {
                    var insertSql = @"
                        INSERT INTO DataRecords (Column1, Column2, Column3, Column4, Column5, ImportedAt, ImportBatch)
                        VALUES (@Column1, @Column2, @Column3, @Column4, @Column5, @ImportedAt, @ImportBatch)";

                    foreach (var record in batch)
                    {
                        using var command = new SqlCommand(insertSql, connection, transaction);
                        command.Parameters.AddWithValue("@Column1", record.Column1 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Column2", record.Column2 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Column3", record.Column3 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Column4", record.Column4 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Column5", record.Column5 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ImportedAt", record.ImportedAt);
                        command.Parameters.AddWithValue("@ImportBatch", record.ImportBatch);
                        command.CommandTimeout = _settings.CommandTimeout;
                        
                        await command.ExecuteNonQueryAsync();
                        totalInserted++;
                    }

                    await transaction.CommitAsync();
                    _logger.LogDebug("Inserted batch of {BatchSize} records", batch.Count());
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            _logger.LogInformation("Successfully inserted {TotalRecords} data records", totalInserted);
        });
    }

    public async Task InsertProcessedFileRecordAsync(ProcessedFileRecord record)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var insertSql = @"
                INSERT INTO ProcessedFiles (FileName, FilePath, FileSize, ProcessedAt, RecordsProcessed, RecordsFailed, Status, ErrorMessage, ProcessingDurationMs)
                OUTPUT INSERTED.Id
                VALUES (@FileName, @FilePath, @FileSize, @ProcessedAt, @RecordsProcessed, @RecordsFailed, @Status, @ErrorMessage, @ProcessingDurationMs)";

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("@FileName", record.FileName);
            command.Parameters.AddWithValue("@FilePath", record.FilePath);
            command.Parameters.AddWithValue("@FileSize", record.FileSize);
            command.Parameters.AddWithValue("@ProcessedAt", record.ProcessedAt);
            command.Parameters.AddWithValue("@RecordsProcessed", record.RecordsProcessed);
            command.Parameters.AddWithValue("@RecordsFailed", record.RecordsFailed);
            command.Parameters.AddWithValue("@Status", record.Status);
            command.Parameters.AddWithValue("@ErrorMessage", record.ErrorMessage ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ProcessingDurationMs", (long)record.ProcessingDuration.TotalMilliseconds);
            command.CommandTimeout = _settings.CommandTimeout;

            var insertedId = await command.ExecuteScalarAsync();
            record.Id = Convert.ToInt32(insertedId);
            
            _logger.LogInformation("Inserted processed file record with ID: {Id}", record.Id);
        });
    }

    public async Task UpdateProcessedFileRecordAsync(int recordId, int recordsProcessed, int recordsFailed, string status, string? errorMessage, TimeSpan duration)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var updateSql = @"
                UPDATE ProcessedFiles 
                SET RecordsProcessed = @RecordsProcessed,
                    RecordsFailed = @RecordsFailed,
                    Status = @Status,
                    ErrorMessage = @ErrorMessage,
                    ProcessingDurationMs = @ProcessingDurationMs
                WHERE Id = @Id";

            using var command = new SqlCommand(updateSql, connection);
            command.Parameters.AddWithValue("@Id", recordId);
            command.Parameters.AddWithValue("@RecordsProcessed", recordsProcessed);
            command.Parameters.AddWithValue("@RecordsFailed", recordsFailed);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@ErrorMessage", errorMessage ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ProcessingDurationMs", (long)duration.TotalMilliseconds);
            command.CommandTimeout = _settings.CommandTimeout;

            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Updated processed file record ID: {Id}", recordId);
        });
    }

    private async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        var attempts = 0;
        while (attempts < _settings.MaxRetryAttempts)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempts < _settings.MaxRetryAttempts - 1 && _settings.EnableRetry)
            {
                attempts++;
                _logger.LogWarning(ex, "Database operation failed (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds} seconds...", 
                    attempts, _settings.MaxRetryAttempts, _settings.MaxRetryAttempts);
                
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts))); // Exponential backoff
            }
        }
    }
}
