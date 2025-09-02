resource "google_artifact_registry_repository" "containers" {
	location      = var.region
	repository_id = var.artifact_repo
	description   = "Container images for Cloud Run services"
	format        = "DOCKER"
}

resource "google_cloud_run_v2_service" "api" {
	name     = "api-app"
	location = var.region
	template {
		containers {
			image = "${var.region}-docker.pkg.dev/${var.project_id}/${var.artifact_repo}/api-app:latest"
			ports {
				container_port = 8080
			}
		}
		scaling {
			min_instance_count = 0
			max_instance_count = 4
		}
	}
	ingress = "INGRESS_TRAFFIC_ALL"
}

resource "google_cloud_run_v2_service" "web" {
	name     = "web-app"
	location = var.region
	template {
		containers {
			image = "${var.region}-docker.pkg.dev/${var.project_id}/${var.artifact_repo}/web-app:latest"
			ports {
				container_port = 8080
			}
		}
		scaling {
			min_instance_count = 0
			max_instance_count = 2
		}
	}
	ingress = "INGRESS_TRAFFIC_ALL"
}

resource "google_cloud_run_v2_job" "batch" {
	name     = "batch-app"
	location = var.region
	template {
		template {
			containers {
				image = "${var.region}-docker.pkg.dev/${var.project_id}/${var.artifact_repo}/batch-app:latest"
			}
		}
	}
}

