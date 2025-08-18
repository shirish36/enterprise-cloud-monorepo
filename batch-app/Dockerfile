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
RUN dotnet publish "batch-app.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Install required packages for GCS FUSE (if needed for volume mounting)
RUN apt-get update && apt-get install -y \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories for GCS bucket mounting with proper permissions
RUN mkdir -p /mnt/gcs-bucket /mnt/gcs-bucket/processed /mnt/gcs-bucket/failed \
    && chown -R appuser:appuser /mnt/gcs-bucket

# Copy the published application
COPY --from=publish /app/publish .

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

# Define the entry point for the container
ENTRYPOINT ["dotnet", "batch-app.dll"]
