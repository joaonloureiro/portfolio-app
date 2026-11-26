# syntax=docker/dockerfile:1.7

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy project manifests and lock files first so dependency restore stays cacheable.
COPY ["global.json", "."]
COPY ["JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj", "JoaoLoureiro.Portfolio.Api/"]
COPY ["JoaoLoureiro.Portfolio.Api/packages.lock.json", "JoaoLoureiro.Portfolio.Api/"]
COPY ["JoaoLoureiro.Portfolio.Application/JoaoLoureiro.Portfolio.Core.csproj", "JoaoLoureiro.Portfolio.Application/"]
COPY ["JoaoLoureiro.Portfolio.Application/packages.lock.json", "JoaoLoureiro.Portfolio.Application/"]
COPY ["JoaoLoureiro.Portfolio.Domain/JoaoLoureiro.Portfolio.Domain.csproj", "JoaoLoureiro.Portfolio.Domain/"]
COPY ["JoaoLoureiro.Portfolio.Domain/packages.lock.json", "JoaoLoureiro.Portfolio.Domain/"]
COPY ["JoaoLoureiro.Portfolio.Infrastructure/JoaoLoureiro.Portfolio.Infrastructure.csproj", "JoaoLoureiro.Portfolio.Infrastructure/"]
COPY ["JoaoLoureiro.Portfolio.Infrastructure/packages.lock.json", "JoaoLoureiro.Portfolio.Infrastructure/"]

RUN dotnet restore "JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj" --locked-mode

COPY . .

RUN dotnet publish "JoaoLoureiro.Portfolio.Api/JoaoLoureiro.Portfolio.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    -p:UseAppHost=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    && case "$TARGETARCH" in \
         amd64) keep_rid="linux-x64" ;; \
         arm64) keep_rid="linux-arm64" ;; \
         *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
       esac \
    && if [ -d /app/publish/runtimes ]; then \
         find /app/publish/runtimes -mindepth 1 -maxdepth 1 ! -name "$keep_rid" ! -name "unix" -exec rm -rf {} + ; \
       fi \
    && mkdir -p /var/lib/portfolio

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build --chown=$APP_UID:$APP_UID /var/lib/portfolio /var/lib/portfolio
COPY --from=build /app/publish .

# The chiseled .NET 8 runtime image runs as non-root app user (UID 1654) without a shell or package manager.
USER $APP_UID

ENTRYPOINT ["dotnet", "JoaoLoureiro.Portfolio.Api.dll"]
