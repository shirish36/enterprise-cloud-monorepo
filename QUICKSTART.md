# Quick Start Guide

## Local Development Setup

### Prerequisites
- .NET 8 SDK installed
- Docker installed
- Google Cloud SDK (optional, for deployment)

### Running the Applications

1. **Start API Service**:
   ```bash
   cd api-app
   dotnet run
   # API will be available at http://localhost:8080
   ```

2. **Start Web Service** (in new terminal):
   ```bash
   cd web-app
   export API_URL=http://localhost:8080  # Linux/Mac
   # or
   set API_URL=http://localhost:8080     # Windows
   dotnet run --urls "http://localhost:8081"
   # Web app will be available at http://localhost:8081
   ```

3. **Start Batch Service** (in new terminal):
   ```bash
   cd batch-app
   dotnet run
   # Check logs for batch processing output
   ```

### Testing the Setup

1. **API Health Check**: http://localhost:8080/health
2. **API Products**: http://localhost:8080/api/products
3. **API Orders**: http://localhost:8080/api/orders
4. **Web Application**: http://localhost:8081
5. **API Documentation**: http://localhost:8080/swagger (if in development mode)

## Docker Development

### Build and Run with Docker

```bash
# API Service
cd api-app
docker build -t enterprise-api .
docker run -p 8080:8080 enterprise-api

# Web Service
cd web-app
docker build -t enterprise-web .
docker run -p 8081:8080 -e API_URL=http://host.docker.internal:8080 enterprise-web

# Batch Service
cd batch-app
docker build -t enterprise-batch .
docker run enterprise-batch
```

### Using Docker Compose (Optional)

Create a `docker-compose.yml` file in the root directory:

```yaml
version: '3.8'
services:
  api:
    build: ./api-app
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
  
  web:
    build: ./web-app
    ports:
      - "8081:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - API_URL=http://api:8080
    depends_on:
      - api
  
  batch:
    build: ./batch-app
    environment:
      - DOTNET_ENVIRONMENT=Development
```

Then run: `docker-compose up`

## Cloud Deployment

### 1. Setup GCP Project

```bash
# Create new project (optional)
gcloud projects create your-project-id

# Set project
gcloud config set project your-project-id

# Enable billing (required)
# Do this through GCP Console

# Enable required APIs
gcloud services enable run.googleapis.com
gcloud services enable containerregistry.googleapis.com
gcloud services enable cloudbuild.googleapis.com
```

### 2. Create Service Account

```bash
# Create service account
gcloud iam service-accounts create cloud-run-deploy \
  --display-name="Cloud Run Deploy Service Account"

# Grant necessary roles
gcloud projects add-iam-policy-binding your-project-id \
  --member="serviceAccount:cloud-run-deploy@your-project-id.iam.gserviceaccount.com" \
  --role="roles/run.admin"

gcloud projects add-iam-policy-binding your-project-id \
  --member="serviceAccount:cloud-run-deploy@your-project-id.iam.gserviceaccount.com" \
  --role="roles/storage.admin"

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
