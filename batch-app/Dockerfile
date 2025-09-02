## batch-app - Cloud Run Job ready Dockerfile (multi-stage)
# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "BatchApp.dll"]

