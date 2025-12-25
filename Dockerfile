# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY backend/Portal.Core/Portal.Core.csproj Portal.Core/
COPY backend/Portal.Infrastructure/Portal.Infrastructure.csproj Portal.Infrastructure/
COPY backend/Portal.API/Portal.API.csproj Portal.API/
RUN dotnet restore Portal.API/Portal.API.csproj

# Copy everything and build
COPY backend/ .
RUN dotnet publish Portal.API/Portal.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Cloud Run uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Portal.API.dll"]
