# Infrastructure as Code

Terraform configuration for deploying the enterprise application stack to Google Cloud Platform.

## Overview

This Terraform configuration provisions:
- **Cloud Run Services**: API, Web, and Batch applications
- **Service Accounts**: With least-privilege access
- **IAM Policies**: For service authentication and authorization
- **API Enablement**: Required Google Cloud APIs
- **Cloud Scheduler**: For batch job scheduling

## Architecture

```
Google Cloud Project
├── Cloud Run Services
│   ├── enterprise-app-api-{env}
│   ├── enterprise-app-web-{env}
│   └── enterprise-app-batch-{env}
├── Service Account
│   └── cloud-run-sa (with logging/monitoring roles)
├── IAM Policies
│   ├── API Service (public access)
│   ├── Web Service (public access)
│   └── Batch Service (internal access)
└── Cloud Scheduler
    └── batch-scheduler (triggers batch jobs)
```

## Files

- **`main.tf`**: Primary resource definitions
- **`variables.tf`**: Input variable declarations
- **`outputs.tf`**: Output value definitions
- **`backend.tf`**: Terraform state backend configuration

## Variables

### Required
- `project_id`: GCP project ID

### Optional  
- `region`: GCP region (default: us-central1)
- `environment`: Environment name (default: prod)
- `app_name`: Application prefix (default: enterprise-app)
- `container_registry`: Registry URL (default: gcr.io)
- `image_tag`: Container image tag (default: latest)

### Resource Configuration
- `api_cpu`/`api_memory`: API service resources
- `web_cpu`/`web_memory`: Web service resources  
- `batch_cpu`/`batch_memory`: Batch service resources
- `min_instances`/`max_instances`: Scaling configuration

## Usage

### 1. Initialize Terraform

```bash
cd infra
terraform init
```

### 2. Configure Variables

Create `terraform.tfvars`:
```hcl
project_id = "your-gcp-project-id"
region     = "us-central1"
environment = "prod"
```

### 3. Plan Deployment

```bash
terraform plan
```

### 4. Apply Configuration

```bash
terraform apply
```

### 5. View Outputs

```bash
terraform output
```

## Outputs

- `api_service_url`: API service endpoint
- `web_service_url`: Web application endpoint  
- `batch_service_url`: Batch service endpoint
- `service_account_email`: Service account email
- `container_registry_urls`: Registry URLs for each service

## Environment Support

Deploy to different environments:

```bash
# Development
terraform apply -var="environment=dev" -var="min_instances=0" -var="max_instances=5"

# Staging  
terraform apply -var="environment=staging" -var="min_instances=1" -var="max_instances=8"

# Production
terraform apply -var="environment=prod" -var="min_instances=2" -var="max_instances=20"
```

## Required APIs

The configuration automatically enables:
- Cloud Run API (`run.googleapis.com`)
- Container Registry API (`containerregistry.googleapis.com`)
- Cloud Build API (`cloudbuild.googleapis.com`)
- Cloud Logging API (`logging.googleapis.com`)
- Cloud Monitoring API (`monitoring.googleapis.com`)

## Service Account Permissions

The created service account has:
- `roles/logging.logWriter`: Write logs to Cloud Logging
- `roles/monitoring.metricWriter`: Write metrics to Cloud Monitoring

## Security Configuration

- **Public Access**: API and Web services allow unauthenticated access
- **Private Access**: Batch service requires authentication
- **Service Account**: Dedicated account with minimal permissions
- **Container Security**: Non-root user execution

## Scaling Configuration

### API/Web Services
- **Min Instances**: 0 (scale to zero)
- **Max Instances**: 10 (auto-scale based on load)
- **CPU Throttling**: Disabled for consistent performance

### Batch Service
- **Min Instances**: 1 (always running)
- **Max Instances**: 1 (single worker)
- **Higher Resources**: 2 CPU cores, 1GB memory

## Cost Optimization

- **Scale to Zero**: API/Web services scale down when idle
- **Right-sizing**: Appropriate CPU/memory allocation per service
- **Regional Deployment**: Single region deployment
- **Minimal Permissions**: Least-privilege service accounts

## Monitoring

All services include:
- Health check endpoints
- Cloud Logging integration
- Cloud Monitoring metrics
- Error reporting

## Backup and State

### Remote State (Recommended)

Uncomment and configure in `backend.tf`:
```hcl
terraform {
  backend "gcs" {
    bucket = "your-terraform-state-bucket"
    prefix = "terraform/state"
  }
}
```

### Local State (Development)

For local development, Terraform uses local state files.

## Troubleshooting

### Common Issues

1. **API not enabled**: Run `gcloud services enable run.googleapis.com`
2. **Permission denied**: Check service account roles
3. **Resource quotas**: Verify Cloud Run quotas in your project
4. **Image not found**: Ensure container images are pushed to registry

### Useful Commands

```bash
# Check service status
gcloud run services list --region=us-central1

# View service logs
gcloud logs read --service=enterprise-app-api-prod

# Destroy infrastructure
terraform destroy
```
