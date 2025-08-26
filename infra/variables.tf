variable "project_id" {
  description = "The GCP project ID"
  type        = string
}

variable "region" {
  description = "The GCP region for resources"
  type        = string
  default     = "us-central1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "app_name" {
  description = "Application name prefix"
  type        = string
  default     = "enterprise-app"
}

variable "container_registry" {
  description = "Container registry URL"
  type        = string
  default     = "trialc4bzjs.jfrog.io"
}

variable "image_tag" {
  description = "Container image tag"
  type        = string
  default     = "latest"
}

variable "api_cpu" {
  description = "CPU allocation for API service"
  type        = string
  default     = "1000m"
}

variable "api_memory" {
  description = "Memory allocation for API service"
  type        = string
  default     = "512Mi"
}

variable "web_cpu" {
  description = "CPU allocation for Web service"
  type        = string
  default     = "1000m"
}

variable "web_memory" {
  description = "Memory allocation for Web service"
  type        = string
  default     = "512Mi"
}

variable "batch_cpu" {
  description = "CPU allocation for Batch service"
  type        = string
  default     = "2000m"
}

variable "batch_memory" {
  description = "Memory allocation for Batch service"
  type        = string
  default     = "1Gi"
}

variable "min_instances" {
  description = "Minimum number of instances"
  type        = number
  default     = 0
}

variable "max_instances" {
  description = "Maximum number of instances"
  type        = number
  default     = 10
}

variable "sql_connection_string" {
  description = "SQL Server connection string"
  type        = string
  default     = ""
  sensitive   = true
}

variable "batch_processing_mode" {
  description = "Batch processing mode (continuous or single)"
  type        = string
  default     = "continuous"
  validation {
    condition     = contains(["continuous", "single"], var.batch_processing_mode)
    error_message = "Batch processing mode must be either 'continuous' or 'single'."
  }
}

variable "batch_job_timeout_minutes" {
  description = "Timeout for batch job execution in minutes"
  type        = number
  default     = 60
}
