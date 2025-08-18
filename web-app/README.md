# Web Application

A .NET 8 Web Application providing a user interface for the enterprise application.

## Features

- **HTML Interface**: Simple, responsive web interface
- **API Integration**: Communicates with API service
- **Health Checks**: `/health` endpoint for monitoring
- **Fallback Handling**: Graceful degradation when API is unavailable
- **Google Cloud Integration**: Logging and monitoring
- **Docker Support**: Multi-stage optimized Dockerfile

## Pages

### Main Interface (`/`)
- System status display
- API integration buttons (Load Products, Load Orders)
- Real-time data display
- Error handling

### Health Check
- `GET /health` - Returns service health status

### API Proxy Endpoints
- `GET /api/products` - Proxies to API service
- `GET /api/orders` - Proxies to API service

## Configuration

Environment variables:
- `PORT`: Server port (default: 8080)
- `API_URL`: URL of the API service
- `GOOGLE_CLOUD_PROJECT`: GCP project ID for logging
- `ENVIRONMENT`: Environment name (dev/staging/prod)

## Local Development

```bash
# Restore and run
dotnet restore
dotnet run

# With API service URL
export API_URL=http://localhost:8080
dotnet run --urls "http://localhost:8081"

# Build and test
dotnet build
dotnet test
```

## Docker

```bash
# Build
docker build -t enterprise-web .

# Run
docker run -p 8081:8080 enterprise-web

# Run with API URL
docker run -p 8081:8080 \
  -e API_URL=http://api-service:8080 \
  -e GOOGLE_CLOUD_PROJECT=your-project \
  enterprise-web
```

## Cloud Run Deployment

```bash
# Build and push to GCR
docker build -t gcr.io/PROJECT_ID/enterprise-app-web:latest .
docker push gcr.io/PROJECT_ID/enterprise-app-web:latest

# Deploy to Cloud Run
gcloud run deploy enterprise-app-web-prod \
  --image gcr.io/PROJECT_ID/enterprise-app-web:latest \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars API_URL=https://api-service-url \
  --port 8080
```

## Dependencies

- Microsoft.AspNetCore.OpenApi
- Google.Cloud.Diagnostics.AspNetCore3

## Architecture

The web application serves as a frontend that:
- Provides a user interface
- Proxies requests to the API service
- Handles service failures gracefully
- Displays real-time system information

## Integration

The web app integrates with the API service using:
- HTTP client factory
- Environment-based configuration
- Fallback error handling
- Health check monitoring
