# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project files first so dependency restore stays cacheable.
COPY ["global.json", "."]
COPY ["JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj", "JoaoLoureiro.Portfolio.Api/"]
COPY ["JoaoLoureiro.Portfolio.Application/JoaoLoureiro.Portfolio.Core.csproj", "JoaoLoureiro.Portfolio.Application/"]
COPY ["JoaoLoureiro.Portfolio.Infrastructure/JoaoLoureiro.Portfolio.Infrastructure.csproj", "JoaoLoureiro.Portfolio.Infrastructure/"]

RUN dotnet restore "JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj"

COPY . .

RUN dotnet publish "JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

# The .NET 8 runtime image provides the non-root app user.
USER $APP_UID

ENTRYPOINT ["dotnet", "JoaoLoureiro.Portfolio.Api.dll"]
