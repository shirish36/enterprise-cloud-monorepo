# Quick Start Guide - Enterprise Cloud Monorepo

This guide will help you deploy the enterprise application stack to Google Cloud Platform using GitHub Actions and Cloud Run.

## 📋 Prerequisites

1. **Google Cloud Project** with billing enabled
2. **GitHub Repository** (we'll create this)
3. **Local Development Environment**:
   - Git installed
   - Google Cloud SDK installed
   - .NET 8 SDK (for local development)

## 🚀 Step-by-Step Deployment

### Step 1: Google Cloud Setup

#### 1.1 Create GCP Project (if needed)
```bash
# Set your project ID
export PROJECT_ID="your-unique-project-id"

# Create new project
gcloud projects create $PROJECT_ID

# Set as current project
gcloud config set project $PROJECT_ID

# Enable billing (required)
# Go to: https://console.cloud.google.com/billing
```

#### 1.2 Enable Required APIs
```bash
gcloud services enable run.googleapis.com
gcloud services enable containerregistry.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable cloudscheduler.googleapis.com
gcloud services enable logging.googleapis.com
gcloud services enable monitoring.googleapis.com
```

#### 1.3 Create Service Account
```bash
# Create service account for GitHub Actions
gcloud iam service-accounts create github-actions-sa \
    --display-name="GitHub Actions Service Account"

# Grant necessary roles
gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:github-actions-sa@$PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/run.admin"

gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:github-actions-sa@$PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/storage.admin"

gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:github-actions-sa@$PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/cloudbuild.builds.editor"

gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:github-actions-sa@$PROJECT_ID.iam.gserviceaccount.com" \
    --role="roles/iam.serviceAccountUser"

# Create and download service account key
gcloud iam service-accounts keys create github-actions-key.json \
    --iam-account=github-actions-sa@$PROJECT_ID.iam.gserviceaccount.com
```

### Step 2: Database Setup (Choose One)

#### Option A: Azure SQL Database
```sql
-- Create database and user
CREATE DATABASE BatchProcessing;
CREATE LOGIN batch_user WITH PASSWORD = 'YourSecurePassword123!';
USE BatchProcessing;
CREATE USER batch_user FOR LOGIN batch_user;
ALTER ROLE db_datareader ADD MEMBER batch_user;
ALTER ROLE db_datawriter ADD MEMBER batch_user;
ALTER ROLE db_ddladmin ADD MEMBER batch_user;
```

Connection String:
```
Server=your-server.database.windows.net;Database=BatchProcessing;User Id=batch_user;Password=YourSecurePassword123!;Encrypt=true;TrustServerCertificate=false;
```

#### Option B: Google Cloud SQL
```bash
# Create Cloud SQL instance
gcloud sql instances create batch-processing-db \
    --database-version=SQLSERVER_2019_STANDARD \
    --tier=db-custom-2-4096 \
    --region=us-central1

# Set root password
gcloud sql users set-password sqlserver \
    --instance=batch-processing-db \
    --password=YourSecurePassword123!

# Create database
gcloud sql databases create BatchProcessing \
    --instance=batch-processing-db
```

### Step 3: GitHub Repository Setup

#### 3.1 Create GitHub Repository
```bash
# Using GitHub CLI (install from: https://cli.github.com/)
gh auth login
gh repo create enterprise-cloud-monorepo --public --clone

# Or create manually at: https://github.com/new
```

```bash
# Navigate to your project directory
cd f:\DevOps_2025\enterprise-cloud-monorepo

# Add remote origin (replace with your GitHub username)
git remote add origin https://github.com/YOUR-USERNAME/enterprise-cloud-monorepo.git

# Push to GitHub
git branch -M main
git push -u origin main
```

#### 3.3 Configure GitHub Secrets
Go to your GitHub repository → Settings → Secrets and variables → Actions

**Required Secrets:**
```
GCP_SA_KEY: [Content of github-actions-key.json file]
GCP_PROJECT_ID: your-gcp-project-id
SQL_CONNECTION_STRING: [Your database connection string from Step 2]
```

**Required Variables:**
```
GCP_REGION: us-central1
```

### Step 4: Infrastructure Deployment

#### 4.1 Configure Terraform Variables
```bash
# Create terraform.tfvars in infra directory
cd infra
cp terraform.tfvars.example terraform.tfvars

# Edit terraform.tfvars
cat > terraform.tfvars << EOF
project_id = "your-gcp-project-id"
region = "us-central1"
environment = "prod"
sql_connection_string = "your-connection-string-here"
EOF
```

#### 4.2 Deploy Infrastructure
```bash
# Option 1: Using GitHub Actions (Recommended)
# Go to GitHub → Actions → Deploy Infrastructure → Run workflow

# Option 2: Manual Terraform Deployment
terraform init
terraform plan
terraform apply
```

### Step 5: Application Deployment

#### 5.1 Automatic Deployment
Once you've pushed to GitHub and configured secrets, deployments will trigger automatically:

- **Infrastructure**: On changes to `infra/**` files
- **API Service**: On changes to `api-app/**` files
- **Web Service**: On changes to `web-app/**` files  
- **Batch Service**: On changes to `batch-app/**` files

#### 5.2 Manual Deployment
```bash
# Go to GitHub → Actions → Choose workflow → Run workflow
# Or trigger manually:

# Build and push batch app
cd batch-app
docker build -t gcr.io/$PROJECT_ID/enterprise-app-batch:latest .
docker push gcr.io/$PROJECT_ID/enterprise-app-batch:latest

# Update Cloud Run Job
gcloud run jobs update enterprise-app-batch-job-prod \
    --image gcr.io/$PROJECT_ID/enterprise-app-batch:latest \
    --region us-central1
```

### Step 6: Test Your Deployment

#### 6.1 Test Web Application
```bash
# Get Web App URL
WEB_URL=$(gcloud run services describe enterprise-app-web-prod \
    --region us-central1 --format="value(status.url)")

echo "Web App URL: $WEB_URL"
curl $WEB_URL/health
```

#### 6.2 Test API Service
```bash
# Get API URL
API_URL=$(gcloud run services describe enterprise-app-api-prod \
    --region us-central1 --format="value(status.url)")

echo "API URL: $API_URL"
curl $API_URL/api/products
```

#### 6.3 Test Batch Processing
```bash
# Upload test file to GCS bucket
BUCKET_NAME="$PROJECT_ID-enterprise-app-processing-prod"

# Create test CSV file
cat > test-data.csv << EOF
Column1,Column2,Column3,Column4,Column5
Test1,Test2,Test3,Test4,Test5
Data1,Data2,Data3,Data4,Data5
EOF

# Upload to bucket
gsutil cp test-data.csv gs://$BUCKET_NAME/input/

# Execute batch job
gcloud run jobs execute enterprise-app-batch-job-prod --region us-central1

# Monitor job execution
gcloud run jobs executions list --job=enterprise-app-batch-job-prod --region=us-central1
```

## 🔧 Local Development

### Setup Local Environment
```bash
# Clone repository
git clone https://github.com/YOUR-USERNAME/enterprise-cloud-monorepo.git
cd enterprise-cloud-monorepo

# Setup environment variables
cp batch-app/ENVIRONMENT_CONFIG.md .env.example

# Edit .env file with your local settings
# Set environment variables in your shell
export SQL_CONNECTION_STRING="your-local-connection-string"
export GOOGLE_CLOUD_PROJECT="your-project-id"

# Run applications locally
cd api-app && dotnet run --urls "http://localhost:8080" &
cd web-app && dotnet run --urls "http://localhost:8081" &
cd batch-app && dotnet run
```

## 📊 Monitoring and Management

### View Logs
```bash
# Application logs
gcloud logging read "resource.type=cloud_run_revision" --limit=50

# Batch job logs
gcloud logging read "resource.type=cloud_run_job" --limit=50

# Filter by service
gcloud logging read "resource.labels.service_name=enterprise-app-api-prod" --limit=20
```

### Manage Batch Jobs
```bash
# List job executions
gcloud run jobs executions list --job=enterprise-app-batch-job-prod --region=us-central1

# View execution details
gcloud run jobs executions describe EXECUTION-NAME --region=us-central1

# Cancel running execution
gcloud run jobs executions cancel EXECUTION-NAME --region=us-central1
```

### Database Monitoring
```sql
-- Check recent file processing
SELECT TOP 10 * FROM ProcessedFiles ORDER BY ProcessedAt DESC;

-- Processing statistics
SELECT 
    COUNT(*) as TotalFiles,
    SUM(RecordsProcessed) as TotalRecords,
    AVG(CAST(ProcessingDurationMs AS FLOAT)) as AvgProcessingTimeMs
FROM ProcessedFiles 
WHERE ProcessedAt >= DATEADD(day, -1, GETUTCDATE());
```

## 🚨 Troubleshooting

### Common Issues

1. **Authentication Errors**
   ```bash
   # Re-authenticate gcloud
   gcloud auth login
   gcloud auth application-default login
   ```

2. **Permission Denied**
   ```bash
   # Check service account permissions
   gcloud projects get-iam-policy $PROJECT_ID
   ```

3. **Container Build Failures**
   ```bash
   # Check Cloud Build logs
   gcloud builds list --limit=5
   gcloud builds log BUILD-ID
   ```

4. **Database Connection Issues**
   ```bash
   # Test connection
   gcloud sql connect batch-processing-db --user=sqlserver
   ```

### Getting Help

- **Documentation**: Check README files in each directory
- **Logs**: Use `gcloud logging read` commands above
- **GitHub Issues**: Create issues in your repository
- **Google Cloud Support**: Use Cloud Console support

## 🎯 Next Steps

1. **Set up monitoring alerts**
2. **Configure backup strategies**
3. **Implement additional environments (dev, staging)**
4. **Add more sophisticated file processing logic**
5. **Set up CI/CD for database migrations**

## 🔐 Security Checklist

- [ ] Service account has minimal required permissions
- [ ] Database credentials are secure and rotated
- [ ] GitHub secrets are properly configured
- [ ] Network security rules are in place
- [ ] Logging and monitoring are enabled
- [ ] Container images are scanned for vulnerabilities

---

**Need Help?** Check the README files in each application directory for detailed configuration and troubleshooting guides.

gcloud projects add-iam-policy-binding your-project-id \
  --member="serviceAccount:cloud-run-deploy@your-project-id.iam.gserviceaccount.com" \
  --role="roles/iam.serviceAccountUser"

# Create and download key
gcloud iam service-accounts keys create key.json \
  --iam-account=cloud-run-deploy@your-project-id.iam.gserviceaccount.com
```

### 3. Deploy Infrastructure

```bash
cd infra
terraform init
terraform plan -var="project_id=your-project-id"
terraform apply -var="project_id=your-project-id"
```

### 4. Build and Deploy Applications

```bash
# Configure Docker for GCR
gcloud auth configure-docker

# Build and push API
cd api-app
docker build -t gcr.io/your-project-id/enterprise-app-api:latest .
docker push gcr.io/your-project-id/enterprise-app-api:latest

# Build and push Web
cd ../web-app
docker build -t gcr.io/your-project-id/enterprise-app-web:latest .
docker push gcr.io/your-project-id/enterprise-app-web:latest

# Build and push Batch
cd ../batch-app
docker build -t gcr.io/your-project-id/enterprise-app-batch:latest .
docker push gcr.io/your-project-id/enterprise-app-batch:latest

# Deploy services
gcloud run deploy enterprise-app-api-prod \
  --image gcr.io/your-project-id/enterprise-app-api:latest \
  --region us-central1 \
  --allow-unauthenticated

gcloud run deploy enterprise-app-web-prod \
  --image gcr.io/your-project-id/enterprise-app-web:latest \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars API_URL=$(gcloud run services describe enterprise-app-api-prod --region us-central1 --format 'value(status.url)')

gcloud run deploy enterprise-app-batch-prod \
  --image gcr.io/your-project-id/enterprise-app-batch:latest \
  --region us-central1 \
  --no-allow-unauthenticated \
  --min-instances 1 \
  --max-instances 1
```

### 5. Setup GitHub Actions (Optional)

1. In GitHub repository settings, add these secrets:
   - `GCP_SA_KEY`: Content of the key.json file
   - `GCP_PROJECT_ID`: Your GCP project ID

2. Add this variable:
   - `GCP_REGION`: us-central1 (or your preferred region)

3. Push changes to main branch to trigger deployment

## Troubleshooting

### Common Issues

1. **Port conflicts**: Make sure ports 8080 and 8081 are available
2. **API connection**: Check API_URL environment variable in web service
3. **Docker permission**: Make sure Docker is running and you have permissions
4. **GCP quotas**: Check Cloud Run quotas in your project
5. **Service account**: Ensure service account has proper permissions

### Viewing Logs

```bash
# Local .NET logs
dotnet run --verbosity detailed

# Docker logs
docker logs <container-id>

# Cloud Run logs
gcloud logs read --service=enterprise-app-api-prod --region=us-central1
```

### Health Checks

All services expose `/health` endpoints that return JSON status information.

### Monitoring

- **Local**: Check console output and application logs
- **Cloud**: Use Google Cloud Console > Cloud Run > Service > Logs tab
