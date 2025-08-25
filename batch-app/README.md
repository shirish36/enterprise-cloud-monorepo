
# Batch Processing Service
> **Note:** This batch app only accesses files via local folder paths. The GCS bucket is mounted as a local directory (e.g., `/data/in`) by Cloud Run. The app does not use any GCS SDK, gcsfuse, or direct GCS API access. All file operations are standard .NET file I/O on the local filesystem.
## Recent Changes (August 2025)

- The batch app now only lists files in the `/data/in` mount path; it no longer logs file contents (test logic removed).
- Product and Order CSV processing includes detailed debug/info logging for each file and row.
- Only files named like `Product*.csv` and `Order*.csv` (case-insensitive, no plural, no underscores/dashes) are loaded into the `Products` and `Orders` tables, respectively.
- All other `.csv` files are loaded into the `DataRecords` table.
- `.txt` files are loaded into the `DataRecords` table as delimited text.
- See the troubleshooting section for common issues with file naming and data loading.


A .NET 8 Console Application for processing files from a GCS bucket (mounted as a local directory) and storing data in MS SQL Server, designed for deployment as Cloud Run Jobs with GCS volume mounting.

## GCS Volume Mounting (Cloud Run Jobs)


**How it works:**
- The GCS bucket is mounted by Cloud Run as a local directory (e.g., `/data/in`).
- The batch app only accesses files using standard .NET file I/O APIs (`System.IO`) on the local filesystem.
- The app does not use any GCS SDK, gcsfuse, or direct GCS API access.
- All GCS authentication and mounting is handled by Cloud Run configuration, not by the app.

**Example:**
If you mount your bucket at `/data/in`, the app will process files from `/data/in` as if it were a local folder.

**Environment variables:**
- `INPUT_DIRECTORY`: Input files location (e.g., `/data/in`)
- `PROCESSED_DIRECTORY`: Processed files location (e.g., `/data/processed`)
- `FAILED_DIRECTORY`: Failed files location (e.g., `/data/failed`)

**No GCS logic is required in the .NET code.**

**All file access is local.** The app is completely unaware of GCS; it only sees files in the mounted directory.


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

1. **Scan** input directory for files (no file content logging)
2. **Process** files one by one
3. **Parse** data based on file type and file name:
  - `Product*.csv` → Products table (headers: Name, Description, Price)
  - `Order*.csv` → Orders table (headers: ProductId, Quantity, OrderDate)
  - Other `.csv` → DataRecords table
  - `.txt` → DataRecords table (pipe/tab delimited)
4. **Insert** data into MS SQL Server using parameterized queries and retry logic
5. **Move** processed files to output directory with timestamp
6. **Log** processing statistics and metrics

## Environment Variables

### Required
- `SQL_CONNECTION_STRING`: MS SQL Server connection string

### Optional (with defaults)
- `INPUT_DIRECTORY`: Input files location (default: `/data/in`)
- `OUTPUT_DIRECTORY`: Output directory for processed files (default: `/data/out`)
- `BATCH_PROCESSING_MODE`: `continuous` or `single` (default: `single`)

> **Note:** All file paths are local. Set these to the mount paths provided by your container/orchestrator (e.g., `/data/in` for input, `/data/out` for output). No GCS-specific settings are required in the app.

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
    "InputDirectory": "/data/in",
    "OutputDirectory": "/data/out"
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

 # Set environment variables for local file handling
 export SQL_CONNECTION_STRING="Server=localhost;Database=BatchProcessing;Integrated Security=true;"
 export INPUT_DIRECTORY="./data/in"
 export OUTPUT_DIRECTORY="./data/out"

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
  -v $(pwd)/data/in:/data/in \
  -v $(pwd)/data/out:/data/out \
  -e SQL_CONNECTION_STRING="your-connection-string" \
  -e INPUT_DIRECTORY="/data/in" \
  -e OUTPUT_DIRECTORY="/data/out" \
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
- For Products: Name, Description, Price
- For Orders: ProductId, Quantity, OrderDate
- Other CSVs: Up to 5 columns supported (DataRecords)

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

3. **Files not processing or not loaded into Products/Orders tables**
  - Check file permissions
  - Verify file format and headers
  - Review input directory path
  - Ensure file names start with `Product` or `Order` (no plural, no underscores/dashes)
  - Check logs for debug/info messages about file processing and row parsing

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
