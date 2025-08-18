# Enterprise Cloud Migration Monorepo

This repository contains a complete enterprise application stack designed for deployment on Google Cloud Platform (GCP) using Cloud Run. The solution includes three microservices: API, Web Application, and Batch Processing.

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web App       │────│   API Service   │    │  Batch Service  │
│  (Frontend)     │    │   (Backend)     │    │  (Processing)   │
│  Port: 8080     │    │   Port: 8080    │    │  Long-running   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────────┐
                    │  Google Cloud   │
                    │    Platform     │
                    │   Cloud Run     │
                    └─────────────────┘
```

## 📁 Project Structure

```
enterprise-cloud-monorepo/
├── api-app/                 # .NET 8 Web API
│   ├── Program.cs
│   ├── api-app.csproj
│   ├── appsettings.json
│   └── Dockerfile
├── web-app/                 # .NET 8 Web Application
│   ├── Program.cs
│   ├── web-app.csproj
│   ├── appsettings.json
│   └── Dockerfile
├── batch-app/               # .NET 8 Console Application
│   ├── Program.cs
│   ├── batch-app.csproj
│   ├── appsettings.json
│   └── Dockerfile
├── infra/                   # Terraform Infrastructure
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── backend.tf
└── gha-workflows/           # GitHub Actions CI/CD
    ├── deploy-infra.yml
    ├── deploy-api.yml
    ├── deploy-web.yml
    └── deploy-batch.yml
```

## 🚀 Services

### API Service (`api-app/`)
- **Framework**: .NET 8 Web API
- **Features**: 
  - RESTful endpoints for products and orders
  - Health check endpoint
  - Google Cloud Logging integration
  - CORS enabled
  - OpenAPI/Swagger documentation

### Web Application (`web-app/`)
- **Framework**: .NET 8 Web Application
- **Features**:
  - Simple HTML interface
  - API integration with fallback
  - Health check endpoint
  - Google Cloud Logging integration

### Batch Service (`batch-app/`)
- **Framework**: .NET 8 Console Application
- **Features**:
  - Background processing service
  - Configurable batch processing
  - Google Cloud Logging integration
  - Error handling and retry logic

## 🏗️ Infrastructure (Terraform)

The infrastructure is defined using Terraform and includes:

- **Cloud Run Services**: For all three applications
- **Service Accounts**: With appropriate permissions
- **IAM Policies**: For public access to web services
- **Cloud Scheduler**: For batch job scheduling
- **Required APIs**: Cloud Run, Container Registry, Cloud Build, etc.

### Key Features:
- Auto-scaling (0-10 instances for API/Web, 1 instance for Batch)
- Resource allocation (CPU/Memory per service)
- Environment-specific deployments
- Google Cloud integration

## 🔧 Deployment

### Prerequisites

1. **Google Cloud Project** with billing enabled
2. **Service Account** with appropriate permissions
3. **GitHub Secrets** configured:
   - `GCP_SA_KEY`: Service account JSON key
   - `GCP_PROJECT_ID`: Your GCP project ID
4. **GitHub Variables**:
   - `GCP_REGION`: Target region (default: us-central1)

### Required GCP APIs
The following APIs will be automatically enabled:
- Cloud Run API
- Container Registry API
- Cloud Build API
- Cloud Logging API
- Cloud Monitoring API

### Service Account Permissions
Your service account needs these roles:
- Cloud Run Admin
- Storage Admin (for Container Registry)
- Cloud Build Editor
- Logging Admin
- Monitoring Editor
- Service Account User

### Deployment Steps

1. **Infrastructure First**:
   ```bash
   # Deploy infrastructure
   cd infra
   terraform init
   terraform plan -var="project_id=YOUR_PROJECT_ID"
   terraform apply
   ```

2. **Applications**:
   - Push to `main` branch to trigger automatic deployment
   - Or use manual workflow dispatch for specific environments

### Workflow Triggers

- **Infrastructure**: Changes to `infra/**` files
- **API Service**: Changes to `api-app/**` files  
- **Web Service**: Changes to `web-app/**` files
- **Batch Service**: Changes to `batch-app/**` files

## 🐳 Local Development

### Running Locally with Docker

```bash
# Build and run API service
cd api-app
docker build -t enterprise-api .
docker run -p 8080:8080 enterprise-api

# Build and run Web service  
cd web-app
docker build -t enterprise-web .
docker run -p 8081:8080 -e API_URL=http://localhost:8080 enterprise-web

# Build and run Batch service
cd batch-app
docker build -t enterprise-batch .
docker run enterprise-batch
```

### Running with .NET CLI

```bash
# API Service
cd api-app
dotnet run

# Web Service (in another terminal)
cd web-app
dotnet run --urls "http://localhost:8081"

# Batch Service (in another terminal)
cd batch-app
dotnet run
```

## 🔍 Monitoring and Logging

All services are configured with Google Cloud Logging and Monitoring:

- **Logs**: Centralized logging to Google Cloud Logging
- **Health Checks**: `/health` endpoints for all services
- **Metrics**: Automatic Cloud Run metrics collection
- **Tracing**: Built-in distributed tracing

## 🛡️ Security

- **Service Accounts**: Dedicated service accounts with minimal permissions
- **Non-root Containers**: All containers run as non-root users
- **IAM**: Proper IAM policies for service-to-service communication
- **HTTPS**: Automatic HTTPS termination by Cloud Run

## 📊 Scaling

- **API/Web Services**: Auto-scale 0-10 instances based on traffic
- **Batch Service**: Single dedicated instance for processing
- **CPU Throttling**: Disabled for consistent performance
- **Resource Limits**: Configured per service type

## 🌍 Multi-Environment Support

The deployment supports multiple environments:
- **dev**: Development environment
- **staging**: Staging environment  
- **prod**: Production environment

Each environment has isolated resources and configurations.

## 📝 Next Steps

1. **Configure your GCP project and service account**
2. **Set up GitHub secrets and variables**
3. **Deploy infrastructure using Terraform**
4. **Push code changes to trigger application deployments**
5. **Monitor services using Google Cloud Console**

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
