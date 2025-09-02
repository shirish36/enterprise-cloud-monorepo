## api-app - Cloud Run ready Dockerfile (multi-stage)
# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_EnableDiagnostics=0
COPY --from=build /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiApp.dll"]

