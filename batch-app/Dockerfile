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


# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories for input, processed, and failed files with proper permissions
RUN mkdir -p /data/in /data/processed /data/failed \
    && chown -R appuser:appuser /data

COPY --from=publish /app/publish .
# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh


# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Set environment variables for production (use Cloud Run GCS mount paths)
ENV DOTNET_ENVIRONMENT=Production
ENV INPUT_DIRECTORY=/data/in
ENV PROCESSED_DIRECTORY=/data/processed
ENV FAILED_DIRECTORY=/data/failed

# Health check for Cloud Run Jobs
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD dotnet --version || exit 1

# Use entrypoint script to run app (GCS bucket is mounted at /data/in by Cloud Run)
ENTRYPOINT ["/entrypoint.sh"]
