# Terraform backend configuration for GCS
# Uncomment and configure for production use

terraform {
  backend "gcs" {
    bucket = "tfstate-bucket-283962084457"
    prefix = "terraform/state"
  }
}

# For local development, comment out the above backend configuration
# and Terraform will use local state files
