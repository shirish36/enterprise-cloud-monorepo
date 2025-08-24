# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["batch-app.csproj", "./"]
RUN dotnet restore "batch-app.csproj"

# Copy source code and build the application
COPY . .
RUN dotnet build "batch-app.csproj" -c Release -o /app/build


# Publish the application
FROM build AS publish
COPY appsettings.json ./
RUN dotnet publish "batch-app.csproj" -c Release -o /app/publish /p:UseAppHost=false
COPY appsettings.json /app/publish/

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    gnupg \
    lsb-release \
    fuse \
    && rm -rf /var/lib/apt/lists/*
# Install gcsfuse
RUN export GCSFUSE_REPO=gcsfuse-`lsb_release -c -s` \
    && echo "deb http://packages.cloud.google.com/apt $GCSFUSE_REPO main" | tee /etc/apt/sources.list.d/gcsfuse.list \
    && curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | apt-key add - \
    && apt-get update \
    && apt-get install -y gcsfuse \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories for GCS bucket mounting with proper permissions
RUN mkdir -p /mnt/gcs-bucket /mnt/gcs-bucket/processed /mnt/gcs-bucket/failed \
    && chown -R appuser:appuser /mnt/gcs-bucket

COPY --from=publish /app/publish .
# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables for production
ENV DOTNET_ENVIRONMENT=Production
ENV INPUT_DIRECTORY=/mnt/gcs-bucket
ENV PROCESSED_DIRECTORY=/mnt/gcs-bucket/processed
ENV FAILED_DIRECTORY=/mnt/gcs-bucket/failed

# Health check for Cloud Run Jobs
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD dotnet --version || exit 1

# Use entrypoint script to mount GCS and run app
ENTRYPOINT ["/entrypoint.sh"]
