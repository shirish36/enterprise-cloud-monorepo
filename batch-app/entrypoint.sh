#!/bin/bash
set -e

# Check required env var
if [ -z "$BUCKET_NAME" ]; then
  echo "BUCKET_NAME environment variable is not set."
  exit 1
fi

# Activate service account if key is present (for local/dev)
if [ -n "$GOOGLE_APPLICATION_CREDENTIALS" ] && [ -f "$GOOGLE_APPLICATION_CREDENTIALS" ]; then
  gcloud auth activate-service-account --key-file="$GOOGLE_APPLICATION_CREDENTIALS"
fi

# Mount GCS bucket using gcsfuse
if ! mountpoint -q /mnt/gcs-bucket; then
  echo "Mounting GCS bucket $BUCKET_NAME to /mnt/gcs-bucket..."
  gcsfuse --implicit-dirs "$BUCKET_NAME" /mnt/gcs-bucket
fi

# Run the .NET batch app
exec dotnet batch-app.dll
