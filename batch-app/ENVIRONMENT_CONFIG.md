# Environment Variables Configuration for Batch Processing

## Required Environment Variables

### Database Configuration
- `SQL_CONNECTION_STRING`: Connection string for MS SQL Server
  ```
  Server=your-server.database.windows.net;Database=BatchProcessing;User Id=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false;
  ```

### Google Cloud Configuration
- `GOOGLE_CLOUD_PROJECT`: Your GCP project ID
- `GOOGLE_APPLICATION_CREDENTIALS`: Path to service account key file (for local development)

### File Processing Configuration
- `INPUT_DIRECTORY`: Directory where input files are located (default: /mnt/gcs-bucket/input)
- `PROCESSED_DIRECTORY`: Directory for successfully processed files (default: /mnt/gcs-bucket/processed)
- `FAILED_DIRECTORY`: Directory for failed files (default: /mnt/gcs-bucket/failed)

### Batch Processing Configuration
- `BATCH_PROCESSING_MODE`: "continuous" or "single" (default: continuous)
- `DOTNET_ENVIRONMENT`: "Development", "Staging", or "Production"

## Local Development Environment Variables

Create a `.env` file in the batch-app directory:

```bash
# Database
SQL_CONNECTION_STRING="Server=localhost;Database=BatchProcessing;Integrated Security=true;"

# Google Cloud
GOOGLE_CLOUD_PROJECT="your-gcp-project-id"
GOOGLE_APPLICATION_CREDENTIALS="path/to/service-account-key.json"

# File Processing (for local testing)
INPUT_DIRECTORY="./data/input"
PROCESSED_DIRECTORY="./data/processed"
FAILED_DIRECTORY="./data/failed"

# Processing Mode
BATCH_PROCESSING_MODE="single"
DOTNET_ENVIRONMENT="Development"
```

## Production Environment Variables (Cloud Run Jobs)

Set these in your Terraform variables or Cloud Run Job environment:

```hcl
# terraform.tfvars
sql_connection_string = "Server=your-server.database.windows.net;Database=BatchProcessing;User Id=batch_user;Password=secure_password;Encrypt=true;"
batch_processing_mode = "continuous"
```

## GitHub Actions Secrets

Configure these secrets in your GitHub repository:

- `GCP_SA_KEY`: Service account JSON key
- `GCP_PROJECT_ID`: Your GCP project ID
- `SQL_CONNECTION_STRING`: Database connection string

## Security Notes

1. **Never commit connection strings** to version control
2. **Use environment variables** for all sensitive configuration
3. **Rotate credentials regularly**
4. **Use least-privilege access** for database users
5. **Enable SSL/TLS** for database connections

## Database Setup

### Create Database and User

```sql
-- Create database
CREATE DATABASE BatchProcessing;

-- Create dedicated user for batch processing
CREATE LOGIN batch_user WITH PASSWORD = 'SecurePassword123!';
USE BatchProcessing;
CREATE USER batch_user FOR LOGIN batch_user;

-- Grant necessary permissions
ALTER ROLE db_datareader ADD MEMBER batch_user;
ALTER ROLE db_datawriter ADD MEMBER batch_user;
ALTER ROLE db_ddladmin ADD MEMBER batch_user;  -- For table creation
```

### Connection String Examples

#### SQL Server (Azure SQL Database)
```
Server=your-server.database.windows.net,1433;Initial Catalog=BatchProcessing;Persist Security Info=False;User ID=batch_user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

#### SQL Server (On-premises with Windows Authentication)
```
Server=your-server;Database=BatchProcessing;Integrated Security=true;Connection Timeout=30;
```

#### SQL Server (On-premises with SQL Authentication)
```
Server=your-server;Database=BatchProcessing;User Id=batch_user;Password=your-password;Connection Timeout=30;
```

## File Format Support

The batch processor supports these file formats:

### CSV Files
- Header row required
- Up to 5 columns (Column1-Column5)
- UTF-8 encoding recommended

### Text Files  
- Pipe-delimited (`|`) or Tab-delimited
- Up to 5 columns
- One record per line

### Example CSV Format
```csv
Column1,Column2,Column3,Column4,Column5
Value1,Value2,Value3,Value4,Value5
Data1,Data2,Data3,Data4,Data5
```

### Example Text Format (Pipe-delimited)
```
Value1|Value2|Value3|Value4|Value5
Data1|Data2|Data3|Data4|Data5
```

## Monitoring and Logging

### Application Logs
- Structured logging to Google Cloud Logging
- Metrics for monitoring batch processing performance
- Error tracking and alerting

### Database Monitoring
- Track processing statistics in `ProcessedFiles` table
- Monitor record counts and processing times
- Identify failed files and error patterns

### Cloud Run Job Monitoring
- Monitor job execution through Google Cloud Console
- Set up alerting for job failures
- Track resource usage and performance metrics
