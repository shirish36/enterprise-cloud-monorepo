terraform {
  required_version = ">= 1.0"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

# Enable required APIs
resource "google_project_service" "cloud_run_api" {
  service = "run.googleapis.com"
}

resource "google_project_service" "container_registry_api" {
  service = "containerregistry.googleapis.com"
}

resource "google_project_service" "cloud_build_api" {
  service = "cloudbuild.googleapis.com"
}

resource "google_project_service" "logging_api" {
  service = "logging.googleapis.com"
}

resource "google_project_service" "monitoring_api" {
  service = "monitoring.googleapis.com"
}

# Service Account for Cloud Run services
resource "google_service_account" "cloud_run_sa" {
  account_id   = "${var.app_name}-cloud-run-sa"
  display_name = "Cloud Run Service Account"
  description  = "Service account for Cloud Run services"
}

# Grant necessary permissions to the service account
resource "google_project_iam_member" "cloud_run_sa_logging" {
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.cloud_run_sa.email}"
}

resource "google_project_iam_member" "cloud_run_sa_monitoring" {
  project = var.project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.cloud_run_sa.email}"
}

# API Service
resource "google_cloud_run_service" "api_service" {
  name     = "${var.app_name}-api-${var.environment}"
  location = var.region

  template {
    spec {
      service_account_name = google_service_account.cloud_run_sa.email
      
      containers {
  image = "${var.container_registry}/${var.project_id}/enterprise-api:${var.image_tag}"
        
        resources {
          limits = {
            cpu    = var.api_cpu
            memory = var.api_memory
          }
        }
        
        env {
          name  = "GOOGLE_CLOUD_PROJECT"
          value = var.project_id
        }
        
        env {
          name  = "ENVIRONMENT"
          value = var.environment
        }
        
        ports {
          container_port = 8080
        }
      }
    }
    
    metadata {
      annotations = {
        "autoscaling.knative.dev/minScale" = var.min_instances
        "autoscaling.knative.dev/maxScale" = var.max_instances
        "run.googleapis.com/cpu-throttling" = "false"
      }
    }
  }

  traffic {
    percent         = 100
    latest_revision = true
  }

  depends_on = [google_project_service.cloud_run_api]
}

# Web Service
resource "google_cloud_run_service" "web_service" {
  name     = "${var.app_name}-web-${var.environment}"
  location = var.region

  template {
    spec {
      service_account_name = google_service_account.cloud_run_sa.email
      
      containers {
  image = "${var.container_registry}/${var.project_id}/enterprise-web:${var.image_tag}"
        
        resources {
          limits = {
            cpu    = var.web_cpu
            memory = var.web_memory
          }
        }
        
        env {
          name  = "GOOGLE_CLOUD_PROJECT"
          value = var.project_id
        }
        
        env {
          name  = "ENVIRONMENT"
          value = var.environment
        }
        
        env {
          name  = "API_URL"
          value = google_cloud_run_service.api_service.status[0].url
        }
        
        ports {
          container_port = 8080
        }
      }
    }
    
    metadata {
      annotations = {
        "autoscaling.knative.dev/minScale" = var.min_instances
        "autoscaling.knative.dev/maxScale" = var.max_instances
        "run.googleapis.com/cpu-throttling" = "false"
      }
    }
  }

  traffic {
    percent         = 100
    latest_revision = true
  }

  depends_on = [google_project_service.cloud_run_api, google_cloud_run_service.api_service]
}

# Enable Cloud Scheduler API for job scheduling
resource "google_project_service" "cloud_scheduler_api" {
  service = "cloudscheduler.googleapis.com"
}

# Create GCS bucket for file processing
resource "google_storage_bucket" "processing_bucket" {
  name     = "${var.project_id}-${var.app_name}-processing-${var.environment}"
  location = var.region

  # Enable uniform bucket-level access
  uniform_bucket_level_access = true

  # Lifecycle management
  lifecycle_rule {
    condition {
      age = 30
    }
    action {
      type = "Delete"
    }
  }

  # Versioning for data protection
  versioning {
    enabled = true
  }
}

# Grant bucket access to the service account
resource "google_storage_bucket_iam_member" "bucket_admin" {
  bucket = google_storage_bucket.processing_bucket.name
  role   = "roles/storage.admin"
  member = "serviceAccount:${google_service_account.cloud_run_sa.email}"
}

# Batch Service as Cloud Run Job (instead of service)
resource "google_cloud_run_v2_job" "batch_job" {
  name     = "${var.app_name}-batch-job-${var.environment}"
  location = var.region

  template {
    template {
      service_account = google_service_account.cloud_run_sa.email

      containers {
        image = "${var.container_registry}/${var.project_id}/enterprise-batch:${var.image_tag}"

        resources {
          limits = {
            cpu    = var.batch_cpu
            memory = var.batch_memory
          }
        }

        env {
          name  = "GOOGLE_CLOUD_PROJECT"
          value = var.project_id
        }

        env {
          name  = "ENVIRONMENT"
          value = var.environment
        }

        env {
          name  = "SQL_CONNECTION_STRING"
          value = var.sql_connection_string != "" ? var.sql_connection_string : "Server=localhost;Database=BatchProcessing;Integrated Security=true;"
        }

        env {
          name  = "BATCH_PROCESSING_MODE"
          value = var.batch_processing_mode
        }

        env {
          name  = "BUCKET_NAME"
          value = google_storage_bucket.processing_bucket.name
        }

  # No volume_mounts needed; GCS FUSE is mounted at runtime in the container
      }

  # GCS FUSE volumes are not supported as a Terraform block. Mount in container at runtime using gcsfuse.
    }

    # Job execution configuration
    task_count  = 1
    parallelism = 1
  }

  depends_on = [
    google_project_service.cloud_run_api,
    google_storage_bucket.processing_bucket,
    google_storage_bucket_iam_member.bucket_admin
  ]
}

# IAM policy to allow public access to API and Web services
resource "google_cloud_run_service_iam_member" "api_public_access" {
  service  = google_cloud_run_service.api_service.name
  location = google_cloud_run_service.api_service.location
  role     = "roles/run.invoker"
  member   = "allUsers"
}

resource "google_cloud_run_service_iam_member" "web_public_access" {
  service  = google_cloud_run_service.web_service.name
  location = google_cloud_run_service.web_service.location
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# Cloud Scheduler for batch job (triggers the Cloud Run Job)
resource "google_cloud_scheduler_job" "batch_scheduler" {
  name        = "${var.app_name}-batch-scheduler-${var.environment}"
  description = "Trigger batch processing job"
  schedule    = "0 */6 * * *"  # Every 6 hours
  time_zone   = "UTC"
  region      = var.region

  http_target {
    uri         = "https://${var.region}-run.googleapis.com/apis/run.googleapis.com/v1/namespaces/${var.project_id}/jobs/${google_cloud_run_v2_job.batch_job.name}:run"
    http_method = "POST"
    
    oauth_token {
      service_account_email = google_service_account.cloud_run_sa.email
    }
  }

  depends_on = [
    google_project_service.cloud_scheduler_api,
    google_cloud_run_v2_job.batch_job
  ]
}
