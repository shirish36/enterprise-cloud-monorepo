output "project_id" {
  description = "The GCP project ID"
  value       = var.project_id
}

output "region" {
  description = "The GCP region"
  value       = var.region
}

output "api_service_url" {
  description = "The URL of the API Cloud Run service"
  value       = google_cloud_run_service.api_service.status[0].url
}

output "web_service_url" {
  description = "The URL of the Web Cloud Run service"
  value       = google_cloud_run_service.web_service.status[0].url
}

output "batch_service_url" {
  description = "The name of the Batch Cloud Run Job"
  value       = google_cloud_run_v2_job.batch_job.name
}

output "service_account_email" {
  description = "The email of the Cloud Run service account"
  value       = google_service_account.cloud_run_sa.email
}

output "container_registry_urls" {
  description = "Container registry URLs for each service"
  value = {
    api   = "${var.container_registry}/${var.project_id}/${var.app_name}-api"
    web   = "${var.container_registry}/${var.project_id}/${var.app_name}-web"
    batch = "${var.container_registry}/${var.project_id}/${var.app_name}-batch"
  }
}

output "scheduler_job_name" {
  description = "The name of the Cloud Scheduler job for batch processing"
  value       = google_cloud_scheduler_job.batch_scheduler.name
}

output "processing_bucket_name" {
  description = "The name of the GCS bucket for file processing"
  value       = google_storage_bucket.processing_bucket.name
}

output "processing_bucket_url" {
  description = "The URL of the GCS bucket for file processing"
  value       = google_storage_bucket.processing_bucket.url
}
