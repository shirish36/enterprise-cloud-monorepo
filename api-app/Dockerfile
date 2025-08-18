# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["api-app.csproj", "./"]
RUN dotnet restore "api-app.csproj"

# Copy source code and build the application
COPY . .
RUN dotnet build "api-app.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "api-app.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published application
COPY --from=publish /app/publish .

# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appuser /app
USER appuser

# Expose the port that the application will run on
EXPOSE 8080

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Define the entry point for the container
ENTRYPOINT ["dotnet", "api-app.dll"]
