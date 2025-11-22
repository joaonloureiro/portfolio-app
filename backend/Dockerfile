# Build stage - using .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["*.sln", "./"]
COPY ["JoaoLoureiro.Portfolio.Api/*.csproj", "JoaoLoureiro.Portfolio.Api/"]
COPY ["JoaoLoureiro.Portfolio.Application/*.csproj", "JoaoLoureiro.Portfolio.Application/"]
COPY ["JoaoLoureiro.Portfolio.Domain/*.csproj", "JoaoLoureiro.Portfolio.Domain/"]
COPY ["JoaoLoureiro.Portfolio.Infrastructure/*.csproj", "JoaoLoureiro.Portfolio.Infrastructure/"]

# Restore dependencies
WORKDIR "/src/JoaoLoureiro.Portfolio.Api"
RUN dotnet restore

# Copy remaining source code
WORKDIR "/src"
COPY . .

# Build and publish
WORKDIR "/src/JoaoLoureiro.Portfolio.Api"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl used by docker-compose healthcheck
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

# Configure logging to show in container logs
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV ASPNETCORE_URLS=http://+:3001
ENV ASPNETCORE_HTTP_PORT=3001
ENV ASPNETCORE_LOGGING__CONSOLE__DISABLECOLORS=true

# Expose the port
EXPOSE 3001

# Copy published app
COPY --from=build /app/publish .

# Entry point with enhanced logging
ENTRYPOINT ["dotnet", "JoaoLoureiro.Portfolio.Api.dll"]