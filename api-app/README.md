# API Service

A .NET 8 Web API service providing RESTful endpoints for the enterprise application.

## Features

- **RESTful API**: Products and Orders endpoints
- **Health Checks**: `/health` endpoint for monitoring
- **OpenAPI/Swagger**: API documentation (in development mode)
- **CORS**: Configured for cross-origin requests
- **Google Cloud Integration**: Logging and monitoring
- **Docker Support**: Multi-stage optimized Dockerfile

## Endpoints

### Health Check
- `GET /health` - Returns service health status

### Products
- `GET /api/products` - Returns list of products

### Orders  
- `GET /api/orders` - Returns list of orders

### Documentation
- `GET /swagger` - Swagger UI (development only)

## Configuration

Environment variables:
- `PORT`: Server port (default: 8080)
- `GOOGLE_CLOUD_PROJECT`: GCP project ID for logging
- `ENVIRONMENT`: Environment name (dev/staging/prod)

## Local Development

```bash
# Restore and run
dotnet restore
dotnet run

# With specific port
dotnet run --urls "http://localhost:8080"

# Build and test
dotnet build
dotnet test
```

## Docker

```bash
# Build
docker build -t enterprise-api .

# Run
docker run -p 8080:8080 enterprise-api

# Run with environment variables
docker run -p 8080:8080 \
  -e GOOGLE_CLOUD_PROJECT=your-project \
  enterprise-api
```

## Cloud Run Deployment

```bash
# Build and push to GCR
docker build -t gcr.io/PROJECT_ID/enterprise-app-api:latest .
docker push gcr.io/PROJECT_ID/enterprise-app-api:latest

# Deploy to Cloud Run
gcloud run deploy enterprise-app-api-prod \
  --image gcr.io/PROJECT_ID/enterprise-app-api:latest \
  --region us-central1 \
  --allow-unauthenticated \
  --port 8080
```

## Dependencies

- Microsoft.AspNetCore.OpenApi
- Swashbuckle.AspNetCore  
- Google.Cloud.Diagnostics.AspNetCore3

## Architecture

The API service is stateless and designed for horizontal scaling. It provides:
- JSON responses
- RESTful resource endpoints
- Health monitoring
- Cloud-native logging
