# Terraform Variables Configuration
# This file is used by Terraform to set variable values for your deployment

project_id = "gifted-palace-468618-q5"
region     = "us-central1"
environment = "development"
app_name    = "enterprise-app"
container_registry = "gcr.io"
image_tag   = "latest"

# Resource Allocation (uncomment and adjust as needed)
api_cpu = "1000m"
api_memory = "512Mi"
web_cpu = "1000m"
web_memory = "512Mi"
batch_cpu = "2000m"
batch_memory = "1Gi"

# Scaling Configuration (uncomment and adjust as needed)
min_instances = 0
max_instances = 10
