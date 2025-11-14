# Use the official .NET 9.0 runtime as a parent image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the official .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/CSharpApp.Api/CSharpApp.Api.csproj", "CSharpApp.Api/"]
COPY ["src/CSharpApp.Application/CSharpApp.Application.csproj", "CSharpApp.Application/"]
COPY ["src/CSharpApp.Core/CSharpApp.Core.csproj", "CSharpApp.Core/"]
COPY ["src/CSharpApp.Infrastructure/CSharpApp.Infrastructure.csproj", "CSharpApp.Infrastructure/"]

RUN dotnet restore "CSharpApp.Api/CSharpApp.Api.csproj"

# Copy the rest of the source code
COPY src/ .

# Build the application
RUN dotnet build "CSharpApp.Api/CSharpApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CSharpApp.Api/CSharpApp.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CSharpApp.Api.dll"]