
# Batch Processing Service


A .NET 8 Console Application for processing files from a GCS bucket (mounted as a local directory) and storing data in MS SQL Server, designed for deployment as Cloud Run Jobs with GCS volume mounting.

## GCS Volume Mounting (Cloud Run Jobs)

**How it works:**
- The GCS bucket is mounted by Cloud Run as a local directory (e.g., `/data/in`).
- The .NET batch app does not require any GCS SDK, gcsfuse, or bucket configuration.
- The app simply reads files from the mount path using standard file I/O APIs (`System.IO`).
- All GCS authentication and mounting is handled by Cloud Run configuration, not by the app.

**Example:**
If you mount your bucket at `/data/in`, the app will process files from `/data/in` as if it were a local folder.

**Environment variables:**
- `INPUT_DIRECTORY`: Input files location (e.g., `/data/in`)
- `PROCESSED_DIRECTORY`: Processed files location (e.g., `/data/processed`)
- `FAILED_DIRECTORY`: Failed files location (e.g., `/data/failed`)

**No GCS logic is required in the .NET code.**


## Features

- **GCS Volume Mounting**: No GCS SDK or gcsfuse required; direct access to GCS bucket as a local directory
- **File Processing**: Support for CSV, text, and JSON files
- **Database Integration**: Bulk insert to MS SQL Server with retry logic
- **Error Handling**: Comprehensive error tracking and file management
- **Environment-Driven Configuration**: All settings via environment variables
- **Cloud Run Jobs**: Optimized for scheduled and on-demand execution
- **Monitoring**: Detailed logging and processing metrics

## Architecture


```
GCS Bucket (mounted as /data/in)
├── file1.csv      # Files to be processed
├── ...
```

## File Processing Flow

1. **Scan** input directory for files matching pattern
2. **Process** files in batches (configurable size)
3. **Parse** data based on file type (CSV, text, JSON)
4. **Insert** data into MS SQL Server using bulk operations
5. **Move** processed files to appropriate directory
6. **Log** processing statistics and metrics

## Environment Variables

### Required
- `SQL_CONNECTION_STRING`: MS SQL Server connection string
- `GOOGLE_CLOUD_PROJECT`: GCP project ID for logging


### Optional (with defaults)
- `INPUT_DIRECTORY`: Input files location (`/data/in`)
- `PROCESSED_DIRECTORY`: Processed files location (`/data/processed`)
- `FAILED_DIRECTORY`: Failed files location (`/data/failed`)
- `BATCH_PROCESSING_MODE`: `continuous` or `single` (`continuous`)

## Configuration

### appsettings.json
```json
{
  "BatchSettings": {
    "ProcessIntervalMinutes": 5,
    "MaxRetries": 3,
    "RetryDelaySeconds": 30,
    "EnableContinuousProcessing": true
  },
  "FileProcessing": {
    "FilePattern": "*.csv",
    "DeleteAfterProcessing": false,
    "MaxFilesPerBatch": 10
  },
  "Database": {
    "CommandTimeout": 30,
    "BulkInsertBatchSize": 1000,
    "EnableRetry": true,
    "MaxRetryAttempts": 3
  }
}
```

## Database Schema

### ProcessedFiles Table
```sql
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
```

### DataRecords Table
```sql
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
```

## Local Development

### Prerequisites
- .NET 8 SDK
- MS SQL Server (local or Azure SQL Database)
- Google Cloud SDK (for authentication)

### Setup
```bash
# Restore dependencies
dotnet restore

# Set environment variables
export SQL_CONNECTION_STRING="Server=localhost;Database=BatchProcessing;Integrated Security=true;"
export GOOGLE_CLOUD_PROJECT="your-project-id"
export INPUT_DIRECTORY="./data/input"
export PROCESSED_DIRECTORY="./data/processed"
export FAILED_DIRECTORY="./data/failed"

# Run the application
dotnet run
```

### Testing with Sample Data
```bash
# Create test directories
mkdir -p ./data/input ./data/processed ./data/failed

# Create sample CSV file
echo "Column1,Column2,Column3,Column4,Column5" > ./data/input/sample.csv
echo "Test1,Test2,Test3,Test4,Test5" >> ./data/input/sample.csv
echo "Data1,Data2,Data3,Data4,Data5" >> ./data/input/sample.csv

# Run in single mode for testing
export BATCH_PROCESSING_MODE="single"
dotnet run
```

## Docker Development

```bash
# Build image
docker build -t batch-processor .

# Run with volume mounts for testing
docker run --rm \
  -v $(pwd)/data:/mnt/gcs-bucket \
  -e SQL_CONNECTION_STRING="your-connection-string" \
  -e GOOGLE_CLOUD_PROJECT="your-project-id" \
  -e BATCH_PROCESSING_MODE="single" \
  batch-processor
```

## Cloud Run Jobs Deployment

### Using Terraform
```hcl
# Set variables in terraform.tfvars
sql_connection_string = "Server=your-server;Database=BatchProcessing;User Id=user;Password=pass;"
batch_processing_mode = "continuous"
```

### Manual Deployment
```bash
# Deploy the job
gcloud run jobs create batch-processing-job \
  --image gcr.io/PROJECT_ID/enterprise-app-batch:latest \
  --region us-central1 \
  --set-env-vars GOOGLE_CLOUD_PROJECT=PROJECT_ID \
  --set-env-vars SQL_CONNECTION_STRING="your-connection-string" \
  --set-env-vars BATCH_PROCESSING_MODE="single" \
  --cpu 2000m \
  --memory 1Gi \
  --mount type=cloud-storage,source=your-bucket,target=/mnt/gcs-bucket

# Execute the job
gcloud run jobs execute batch-processing-job --region us-central1
```

## File Format Support

### CSV Files
- Header row required
- Comma-separated values
- UTF-8 encoding
- Up to 5 columns supported

### Text Files
- Pipe-delimited (`|`) or tab-delimited
- One record per line
- No header required

### JSON Files
- Array of objects format
- Placeholder for future implementation

## Monitoring and Alerting

### Logs
```bash
# View job logs
gcloud logging read "resource.type=cloud_run_job" --limit=50

# Filter by specific job
gcloud logging read "resource.labels.job_name=batch-processing-job" --limit=50
```

### Metrics
- Processing time per batch
- Files processed vs failed
- Records processed per minute
- Error rates and types

### Database Queries
```sql
-- Recent processing summary
SELECT 
    FileName,
    RecordsProcessed,
    RecordsFailed,
    Status,
    ProcessingDurationMs,
    ProcessedAt
FROM ProcessedFiles 
WHERE ProcessedAt >= DATEADD(hour, -24, GETUTCDATE())
ORDER BY ProcessedAt DESC;

-- Processing statistics
SELECT 
    COUNT(*) as TotalFiles,
    SUM(RecordsProcessed) as TotalRecordsProcessed,
    SUM(RecordsFailed) as TotalRecordsFailed,
    AVG(ProcessingDurationMs) as AvgProcessingTimeMs
FROM ProcessedFiles 
WHERE ProcessedAt >= DATEADD(day, -1, GETUTCDATE());
```

## Error Handling

### File Processing Errors
- Invalid file formats moved to `failed/` directory
- Processing errors logged with details
- Partial processing supported (some records succeed)

### Database Errors
- Automatic retry with exponential backoff
- Transaction rollback on batch failures
- Connection resilience

### System Errors
- Graceful shutdown on termination signals
- Resource cleanup on errors
- Comprehensive error logging

## Performance Tuning

### Database
- Bulk insert operations (1000 records per batch)
- Connection pooling
- Parallel processing where applicable

### File Processing
- Streaming file readers for large files
- Memory-efficient processing
- Configurable batch sizes

### Resource Management
- CPU: 2000m (2 cores) for processing-intensive tasks
- Memory: 1Gi for handling large files
- Disk: Uses mounted GCS bucket (no local storage)

## Security

### Database
- SQL injection prevention with parameterized queries
- Least-privilege database access
- Encrypted connections

### File Access
- Read-only access to input directory
- Secure file movement operations
- Path traversal prevention

### Cloud Security
- Service account with minimal permissions
- Encrypted environment variables
- VPC security if required

## Troubleshooting

### Common Issues

1. **Cannot connect to database**
   - Verify connection string format
   - Check firewall rules
   - Validate credentials

2. **GCS bucket not mounted**
   - Verify service account permissions
   - Check bucket name and region
   - Review Cloud Run Job configuration

3. **Files not processing**
   - Check file permissions
   - Verify file format
   - Review input directory path

4. **Out of memory errors**
   - Reduce batch size
   - Increase memory allocation
   - Check for memory leaks in file processing

### Debug Commands
```bash
# Check service account permissions
gcloud projects get-iam-policy PROJECT_ID

# Test database connection
sqlcmd -S server -d database -U user -P password -Q "SELECT 1"

# Monitor job execution
gcloud run jobs executions list --job=batch-processing-job --region=us-central1
```
