#!/bin/bash
set -e


# Run the .NET batch app (GCS bucket is mounted at /data/in by Cloud Run)
exec dotnet batch-app.dll
